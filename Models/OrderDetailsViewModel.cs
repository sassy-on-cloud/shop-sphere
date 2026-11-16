namespace ShopSphere.Models;

public sealed class OrderDetailsViewModel
{
    public required int OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required DateTime OrderDate { get; init; }

    public required decimal TotalAmount { get; init; }

    public required string CustomerName { get; init; }

    public required string PhoneNumber { get; init; }

    public required string Province { get; init; }

    public required string District { get; init; }

    public required string City { get; init; }

    public required string StreetAddress { get; init; }

    public string? DeliveryInstructions { get; init; }

    public required PaymentMethod PaymentMethod { get; init; }

    public required PaymentStatus PaymentStatus { get; init; }

    public required OrderStatus OrderStatus { get; init; }

    public required IReadOnlyList<OrderLineViewModel> Items { get; init; }

    public bool CanCancel => OrderStatus == OrderStatus.Pending;
}

public sealed record OrderLineViewModel(
    string ProductName,
    string ProductImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
