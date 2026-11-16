using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Services;

public enum OrderPlacementStatus
{
    Created,
    AlreadyPlaced,
    EmptyCart,
    InsufficientStock,
    AmountTooLarge,
    InvalidSubmission
}

public sealed record OrderPlacementResult(
    OrderPlacementStatus Status,
    int? OrderId = null,
    string? Message = null);

public enum OrderCancellationStatus
{
    Cancelled,
    NotFound,
    NotCancelable
}

public enum OrderStatusUpdateStatus
{
    Updated,
    NotFound,
    InvalidTransition
}

public sealed class OrderService(ShopSphereDbContext dbContext)
{
    private const decimal MaximumOrderAmount = 9999999999999999.99m;

    public async Task<OrderPlacementResult> PlaceOrderAsync(
        string userId,
        CheckoutInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.SubmissionToken == Guid.Empty)
        {
            return new(OrderPlacementStatus.InvalidSubmission, Message: "Refresh checkout and try again.");
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var existingOrderId = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId &&
                                order.SubmissionToken == input.SubmissionToken)
                .Select(order => (int?)order.OrderId)
                .SingleOrDefaultAsync(cancellationToken);

            if (existingOrderId.HasValue)
            {
                return new(OrderPlacementStatus.AlreadyPlaced, existingOrderId.Value);
            }

            var cart = await dbContext.Carts
                .Where(item => item.UserId == userId)
                .Include(item => item.Items)
                .ThenInclude(item => item.Product)
                .SingleOrDefaultAsync(cancellationToken);

            if (cart is null || cart.Items.Count == 0)
            {
                return new(OrderPlacementStatus.EmptyCart, Message: "Your bag is empty.");
            }

            var orderItems = new List<OrderItem>(cart.Items.Count);
            var totalAmount = 0m;
            foreach (var cartItem in cart.Items)
            {
                if (cartItem.Quantity > cartItem.Product.StockQuantity)
                {
                    return new(
                        OrderPlacementStatus.InsufficientStock,
                        Message: $"Only {cartItem.Product.StockQuantity} item(s) of {cartItem.Product.ProductName} are currently available.");
                }

                var unitPrice = ProductPricing.CalculateCurrentPrice(cartItem.Product);
                var subtotal = decimal.Round(
                    unitPrice * cartItem.Quantity,
                    2,
                    MidpointRounding.AwayFromZero);

                if (subtotal > MaximumOrderAmount || totalAmount > MaximumOrderAmount - subtotal)
                {
                    return new(
                        OrderPlacementStatus.AmountTooLarge,
                        Message: "The order total is too large to process. Please reduce quantities and try again.");
                }

                totalAmount += subtotal;
                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.ProductName,
                    ProductImageUrl = cartItem.Product.ImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = subtotal
                });
            }

            var order = new Order
            {
                OrderNumber = CreateOrderNumber(),
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                CustomerName = input.CustomerName.Trim(),
                PhoneNumber = input.PhoneNumber.Trim(),
                Province = input.Province.Trim(),
                District = input.District.Trim(),
                City = input.City.Trim(),
                StreetAddress = input.StreetAddress.Trim(),
                DeliveryInstructions = string.IsNullOrWhiteSpace(input.DeliveryInstructions)
                    ? null
                    : input.DeliveryInstructions.Trim(),
                PaymentMethod = PaymentMethod.CashOnDelivery,
                PaymentStatus = PaymentStatus.Pending,
                OrderStatus = OrderStatus.Pending,
                SubmissionToken = input.SubmissionToken,
                Items = orderItems
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var cartItem in cart.Items)
            {
                var updatedProducts = await dbContext.Products
                    .Where(product =>
                        product.ProductId == cartItem.ProductId &&
                        product.StockQuantity >= cartItem.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            product => product.StockQuantity,
                            product => product.StockQuantity - cartItem.Quantity),
                        cancellationToken);

                if (updatedProducts != 1)
                {
                    return new(
                        OrderPlacementStatus.InsufficientStock,
                        Message: $"Stock for {cartItem.Product.ProductName} changed during checkout. Review your bag and try again.");
                }
            }

            dbContext.CartItems.RemoveRange(cart.Items);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new(OrderPlacementStatus.Created, order.OrderId);
        }
        catch (DbUpdateException exception) when (IsPossibleDuplicateSubmission(exception))
        {
            var existingOrderId = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId &&
                                order.SubmissionToken == input.SubmissionToken)
                .Select(order => (int?)order.OrderId)
                .SingleOrDefaultAsync(cancellationToken);

            if (existingOrderId.HasValue)
            {
                return new(OrderPlacementStatus.AlreadyPlaced, existingOrderId.Value);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<OrderSummaryViewModel>> GetOrdersAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .Select(order => new OrderSummaryViewModel(
                order.OrderId,
                order.OrderNumber,
                order.OrderDate,
                order.TotalAmount,
                order.PaymentMethod,
                order.OrderStatus))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(
        string userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.OrderId == orderId,
                cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderDetailsViewModel
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            CustomerName = order.CustomerName,
            PhoneNumber = order.PhoneNumber,
            Province = order.Province,
            District = order.District,
            City = order.City,
            StreetAddress = order.StreetAddress,
            DeliveryInstructions = order.DeliveryInstructions,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            OrderStatus = order.OrderStatus,
            Items = order.Items
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderLineViewModel(
                    item.ProductName,
                    item.ProductImageUrl,
                    item.Quantity,
                    item.UnitPrice,
                    item.Subtotal))
                .ToList()
        };
    }

    public async Task<OrderCancellationStatus> CancelAsync(
        string userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.OrderId == orderId,
                cancellationToken);

        if (order is null)
        {
            return OrderCancellationStatus.NotFound;
        }

        if (order.OrderStatus != OrderStatus.Pending)
        {
            return OrderCancellationStatus.NotCancelable;
        }

        var changedOrders = await dbContext.Orders
            .Where(item =>
                item.UserId == userId &&
                item.OrderId == orderId &&
                item.OrderStatus == OrderStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.OrderStatus,
                    OrderStatus.Cancelled),
                cancellationToken);

        if (changedOrders != 1)
        {
            return OrderCancellationStatus.NotCancelable;
        }

        await RestoreStockAsync(order.Items, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return OrderCancellationStatus.Cancelled;
    }

    public async Task<OrderStatusUpdateStatus> UpdateStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), nextStatus))
        {
            return OrderStatusUpdateStatus.InvalidTransition;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return OrderStatusUpdateStatus.NotFound;
        }

        if (!IsAllowedTransition(order.OrderStatus, nextStatus))
        {
            return OrderStatusUpdateStatus.InvalidTransition;
        }

        var changedOrders = await dbContext.Orders
            .Where(item => item.OrderId == orderId && item.OrderStatus == order.OrderStatus)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.OrderStatus, nextStatus),
                cancellationToken);

        if (changedOrders != 1)
        {
            return OrderStatusUpdateStatus.InvalidTransition;
        }

        if (nextStatus == OrderStatus.Cancelled)
        {
            await RestoreStockAsync(order.Items, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return OrderStatusUpdateStatus.Updated;
    }

    public static IReadOnlyList<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus) =>
        currentStatus switch
        {
            OrderStatus.Pending => [OrderStatus.Confirmed, OrderStatus.Cancelled],
            OrderStatus.Confirmed => [OrderStatus.Processing, OrderStatus.Cancelled],
            OrderStatus.Processing => [OrderStatus.Shipped, OrderStatus.Cancelled],
            OrderStatus.Shipped => [OrderStatus.Delivered],
            _ => []
        };

    private static string CreateOrderNumber() =>
        $"SS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

    private static bool IsPossibleDuplicateSubmission(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 1205 or 2601 or 2627 };

    private async Task RestoreStockAsync(
        IEnumerable<OrderItem> orderItems,
        CancellationToken cancellationToken)
    {
        foreach (var productGroup in orderItems
                     .Where(item => item.ProductId.HasValue)
                     .GroupBy(item => item.ProductId!.Value))
        {
            var quantity = productGroup.Sum(item => item.Quantity);
            await dbContext.Products
                .Where(product => product.ProductId == productGroup.Key)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        product => product.StockQuantity,
                        product => product.StockQuantity + quantity),
                    cancellationToken);
        }
    }

    private static bool IsAllowedTransition(OrderStatus current, OrderStatus next) =>
        GetAllowedTransitions(current).Contains(next);
}
