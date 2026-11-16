using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Services;

public enum CartOperationStatus
{
    Success,
    InvalidQuantity,
    ProductNotFound,
    InsufficientStock,
    ItemNotFound
}

public sealed record CartOperationResult(CartOperationStatus Status, string Message)
{
    public bool Succeeded => Status == CartOperationStatus.Success;
}

public sealed class ShoppingCartService(ShopSphereDbContext dbContext)
{
    public async Task<CartViewModel> GetCartAsync(
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        var cart = await OwnedCarts(userId, guestToken)
            .AsNoTracking()
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(cancellationToken);

        var items = cart?.Items
            .OrderBy(item => item.AddedAt)
            .Select(item =>
            {
                var price = CurrentPrice(item.Product);
                return new CartItemViewModel(
                    item.CartItemId,
                    item.ProductId,
                    item.Product.ProductName,
                    item.Product.ImageUrl,
                    item.Quantity,
                    item.Product.StockQuantity,
                    price,
                    decimal.Round(price * item.Quantity, 2, MidpointRounding.AwayFromZero));
            })
            .ToList() ?? [];

        return new CartViewModel { Items = items };
    }

    public async Task<int> GetItemCountAsync(
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        var count = await OwnedCarts(userId, guestToken)
            .SelectMany(cart => cart.Items)
            .Select(item => (int?)item.Quantity)
            .SumAsync(cancellationToken);

        return count ?? 0;
    }

    public async Task<CartOperationResult> AddItemAsync(
        string? userId,
        string? guestToken,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
        {
            return new(CartOperationStatus.InvalidQuantity, "Choose a quantity of at least one.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return new(CartOperationStatus.ProductNotFound, "This product is no longer available.");
        }

        var cart = await OwnedCarts(userId, guestToken)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(cancellationToken);

        if (cart is null)
        {
            cart = CreateCart(userId, guestToken);
            if (cart is null)
            {
                return new(CartOperationStatus.InvalidQuantity, "Your guest cart could not be identified. Please try again.");
            }

            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingItem = cart.Items.SingleOrDefault(item => item.ProductId == productId);
        var currentQuantity = existingItem?.Quantity ?? 0;
        if (product.StockQuantity <= 0 || quantity > product.StockQuantity - currentQuantity)
        {
            return new(
                CartOperationStatus.InsufficientStock,
                product.StockQuantity <= 0
                    ? "This product is currently out of stock."
                    : $"Only {product.StockQuantity} item(s) are currently available.");
        }

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.ProductId,
                Quantity = quantity,
                AddedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingItem.Quantity += quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(CartOperationStatus.Success, "Product added to your bag.");
    }

    public async Task<CartOperationResult> ChangeQuantityAsync(
        string? userId,
        string? guestToken,
        int cartItemId,
        int delta,
        CancellationToken cancellationToken = default)
    {
        if (delta is not (-1 or 1))
        {
            return new(CartOperationStatus.InvalidQuantity, "Choose a valid quantity change.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var cart = await OwnedCarts(userId, guestToken)
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(cancellationToken);
        var cartItem = cart?.Items.SingleOrDefault(item => item.CartItemId == cartItemId);
        if (cart is null || cartItem is null)
        {
            return new(CartOperationStatus.ItemNotFound, "That item is not in your bag.");
        }

        var nextQuantity = cartItem.Quantity + delta;
        if (nextQuantity <= 0)
        {
            dbContext.CartItems.Remove(cartItem);
        }
        else if (nextQuantity > cartItem.Product.StockQuantity)
        {
            return new(
                CartOperationStatus.InsufficientStock,
                $"Only {cartItem.Product.StockQuantity} item(s) are currently available.");
        }
        else
        {
            cartItem.Quantity = nextQuantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(CartOperationStatus.Success, "Your bag has been updated.");
    }

    public async Task<CartOperationResult> RemoveItemAsync(
        string? userId,
        string? guestToken,
        int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cart = await OwnedCarts(userId, guestToken)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(cancellationToken);
        var cartItem = cart?.Items.SingleOrDefault(item => item.CartItemId == cartItemId);
        if (cart is null || cartItem is null)
        {
            return new(CartOperationStatus.ItemNotFound, "That item is not in your bag.");
        }

        dbContext.CartItems.Remove(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(CartOperationStatus.Success, "Item removed from your bag.");
    }

    public async Task<CartOperationResult> ClearAsync(
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        var cart = await OwnedCarts(userId, guestToken)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(cancellationToken);
        if (cart is null)
        {
            return new(CartOperationStatus.Success, "Your bag is already empty.");
        }

        dbContext.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(CartOperationStatus.Success, "Your bag has been cleared.");
    }

    public async Task MergeGuestCartAsync(
        string userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(guestToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var guestKey = GuestCartCookie.HashToken(guestToken);
        var guestCart = await dbContext.Carts
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(cart => cart.GuestCartKey == guestKey, cancellationToken);

        if (guestCart is null)
        {
            return;
        }

        var userCart = await dbContext.Carts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);

        if (userCart is null)
        {
            userCart = new Cart { UserId = userId };
            dbContext.Carts.Add(userCart);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var guestItem in guestCart.Items)
        {
            var stock = guestItem.Product.StockQuantity;
            if (stock <= 0)
            {
                continue;
            }

            var userItem = userCart.Items.SingleOrDefault(item => item.ProductId == guestItem.ProductId);
            if (userItem is null)
            {
                userCart.Items.Add(new CartItem
                {
                    ProductId = guestItem.ProductId,
                    Quantity = Math.Min(guestItem.Quantity, stock),
                    AddedAt = guestItem.AddedAt
                });
            }
            else
            {
                userItem.Quantity = userItem.Quantity >= stock - Math.Min(guestItem.Quantity, stock)
                    ? stock
                    : userItem.Quantity + guestItem.Quantity;
            }
        }

        userCart.UpdatedAt = DateTime.UtcNow;
        dbContext.Carts.Remove(guestCart);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    private IQueryable<Cart> OwnedCarts(string? userId, string? guestToken)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            return dbContext.Carts.Where(cart => cart.UserId == userId);
        }

        if (!string.IsNullOrEmpty(guestToken))
        {
            var guestKey = GuestCartCookie.HashToken(guestToken);
            return dbContext.Carts.Where(cart => cart.GuestCartKey == guestKey);
        }

        return dbContext.Carts.Where(_ => false);
    }

    private static Cart? CreateCart(string? userId, string? guestToken)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            return new Cart { UserId = userId };
        }

        return string.IsNullOrEmpty(guestToken)
            ? null
            : new Cart { GuestCartKey = GuestCartCookie.HashToken(guestToken) };
    }

    private static decimal CurrentPrice(Product product) =>
        decimal.Round(
            product.Price * (100m - product.DiscountPercentage) / 100m,
            2,
            MidpointRounding.AwayFromZero);
}
