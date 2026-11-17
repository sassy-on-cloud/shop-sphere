using Microsoft.EntityFrameworkCore;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Tests;

public sealed class CartAndOrderServiceTests
{
    [Fact]
    public async Task CartUsesCurrentDiscountedPriceAndCalculatesSubtotal()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(
            stockQuantity: 5,
            price: 123.45m,
            discountPercentage: 10m);
        var cartService = new ShoppingCartService(database.DbContext);

        var addResult = await cartService.AddItemAsync(user.Id, null, product.ProductId, 2);
        var cart = await cartService.GetCartAsync(user.Id, null);

        Assert.Equal(CartOperationStatus.Success, addResult.Status);
        Assert.Equal(2, cart.TotalItems);
        Assert.Equal(222.22m, cart.TotalPrice);
        Assert.Equal(111.11m, Assert.Single(cart.Items).UnitPrice);
    }

    [Fact]
    public async Task CartRejectsQuantityAboveAvailableStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 2);
        var cartService = new ShoppingCartService(database.DbContext);

        var result = await cartService.AddItemAsync(user.Id, null, product.ProductId, 3);

        Assert.Equal(CartOperationStatus.InsufficientStock, result.Status);
        Assert.Equal(0, await database.DbContext.Carts.CountAsync());
    }

    [Fact]
    public async Task GuestCartMergeNeverExceedsRemainingStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 5);
        var guestToken = "test-guest-token";
        var userCart = new Cart { UserId = user.Id };
        var guestCart = new Cart { GuestCartKey = GuestCartCookie.HashToken(guestToken) };
        userCart.Items.Add(new CartItem { Product = product, Quantity = 4 });
        guestCart.Items.Add(new CartItem { Product = product, Quantity = 3 });
        database.DbContext.Carts.AddRange(userCart, guestCart);
        await database.DbContext.SaveChangesAsync();

        var cartService = new ShoppingCartService(database.DbContext);
        await cartService.MergeGuestCartAsync(user.Id, guestToken);

        var mergedCart = await database.DbContext.Carts
            .Include(cart => cart.Items)
            .SingleAsync(cart => cart.UserId == user.Id);
        Assert.Equal(5, Assert.Single(mergedCart.Items).Quantity);
        Assert.Equal(1, await database.DbContext.Carts.CountAsync());
    }

    [Fact]
    public async Task CheckoutSnapshotsCurrentDiscountedPriceAndUpdatesInventory()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(
            stockQuantity: 8,
            price: 123.45m,
            discountPercentage: 10m);
        var cartService = new ShoppingCartService(database.DbContext);
        await cartService.AddItemAsync(user.Id, null, product.ProductId, 2);
        var orderService = new OrderService(database.DbContext);
        var submissionToken = Guid.NewGuid();

        var result = await orderService.PlaceOrderAsync(user.Id, ValidCheckout(submissionToken));

        Assert.Equal(OrderPlacementStatus.Created, result.Status);
        database.DbContext.ChangeTracker.Clear();
        var order = await database.DbContext.Orders
            .Include(item => item.Items)
            .SingleAsync(item => item.OrderId == result.OrderId);
        Assert.Equal(222.22m, order.TotalAmount);
        Assert.Equal(PaymentMethod.CashOnDelivery, order.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(111.11m, Assert.Single(order.Items).UnitPrice);
        Assert.Equal(6, (await database.DbContext.Products.FindAsync(product.ProductId))!.StockQuantity);
        Assert.Equal(0, await database.DbContext.CartItems.CountAsync());
    }

    [Fact]
    public async Task CheckoutRejectsCartQuantityThatExceedsCurrentStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 1);
        var cart = new Cart { UserId = user.Id };
        cart.Items.Add(new CartItem { Product = product, Quantity = 2 });
        database.DbContext.Carts.Add(cart);
        await database.DbContext.SaveChangesAsync();
        var orderService = new OrderService(database.DbContext);

        var result = await orderService.PlaceOrderAsync(user.Id, ValidCheckout(Guid.NewGuid()));

        Assert.Equal(OrderPlacementStatus.InsufficientStock, result.Status);
        database.DbContext.ChangeTracker.Clear();
        Assert.Equal(0, await database.DbContext.Orders.CountAsync());
        Assert.Equal(1, (await database.DbContext.Products.FindAsync(product.ProductId))!.StockQuantity);
        Assert.Equal(2, await database.DbContext.CartItems
            .Where(item => item.CartId == cart.CartId)
            .Select(item => item.Quantity)
            .SingleAsync());
    }

    [Fact]
    public async Task RepeatedCheckoutSubmissionDoesNotCreateAnotherOrderOrDeductStockAgain()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 5);
        var cartService = new ShoppingCartService(database.DbContext);
        await cartService.AddItemAsync(user.Id, null, product.ProductId, 2);
        var orderService = new OrderService(database.DbContext);
        var input = ValidCheckout(Guid.NewGuid());

        var firstResult = await orderService.PlaceOrderAsync(user.Id, input);
        var repeatedResult = await orderService.PlaceOrderAsync(user.Id, input);

        Assert.Equal(OrderPlacementStatus.Created, firstResult.Status);
        Assert.Equal(OrderPlacementStatus.AlreadyPlaced, repeatedResult.Status);
        Assert.Equal(firstResult.OrderId, repeatedResult.OrderId);
        database.DbContext.ChangeTracker.Clear();
        Assert.Equal(1, await database.DbContext.Orders.CountAsync());
        Assert.Equal(3, (await database.DbContext.Products.FindAsync(product.ProductId))!.StockQuantity);
    }

    [Fact]
    public async Task CheckoutRejectsAnEmptySubmissionToken()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, _) = await database.AddCustomerAndProductAsync();
        var orderService = new OrderService(database.DbContext);

        var result = await orderService.PlaceOrderAsync(user.Id, ValidCheckout(Guid.Empty));

        Assert.Equal(OrderPlacementStatus.InvalidSubmission, result.Status);
        Assert.Equal(0, await database.DbContext.Orders.CountAsync());
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomersOrderDetails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 3);
        var cartService = new ShoppingCartService(database.DbContext);
        await cartService.AddItemAsync(user.Id, null, product.ProductId, 1);
        var orderService = new OrderService(database.DbContext);
        var orderResult = await orderService.PlaceOrderAsync(user.Id, ValidCheckout(Guid.NewGuid()));
        var otherUserId = Guid.NewGuid().ToString("N");
        database.DbContext.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            UserName = "other-customer@example.test",
            NormalizedUserName = "OTHER-CUSTOMER@EXAMPLE.TEST",
            Email = "other-customer@example.test",
            NormalizedEmail = "OTHER-CUSTOMER@EXAMPLE.TEST",
            FullName = "Other Customer",
            CreatedAt = DateTime.UtcNow
        });
        await database.DbContext.SaveChangesAsync();

        var order = await orderService.GetOrderDetailsAsync(otherUserId, orderResult.OrderId!.Value);

        Assert.Null(order);
    }

    [Fact]
    public async Task CancellingOrderRestoresStockOnlyOnce()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await database.AddCustomerAndProductAsync(stockQuantity: 5);
        var cartService = new ShoppingCartService(database.DbContext);
        await cartService.AddItemAsync(user.Id, null, product.ProductId, 2);
        var orderService = new OrderService(database.DbContext);
        var orderResult = await orderService.PlaceOrderAsync(user.Id, ValidCheckout(Guid.NewGuid()));

        var firstCancellation = await orderService.CancelAsync(user.Id, orderResult.OrderId!.Value);
        var repeatedCancellation = await orderService.CancelAsync(user.Id, orderResult.OrderId.Value);

        Assert.Equal(OrderCancellationStatus.Cancelled, firstCancellation);
        Assert.Equal(OrderCancellationStatus.NotCancelable, repeatedCancellation);
        database.DbContext.ChangeTracker.Clear();
        Assert.Equal(5, (await database.DbContext.Products.FindAsync(product.ProductId))!.StockQuantity);
        Assert.Equal(OrderStatus.Cancelled,
            (await database.DbContext.Orders.FindAsync(orderResult.OrderId.Value))!.OrderStatus);
    }

    private static CheckoutInput ValidCheckout(Guid submissionToken) => new()
    {
        SubmissionToken = submissionToken,
        CustomerName = "Test Customer",
        PhoneNumber = "+977-9800000000",
        Province = "Bagmati",
        District = "Kathmandu",
        City = "Kathmandu",
        StreetAddress = "Test street"
    };
}
