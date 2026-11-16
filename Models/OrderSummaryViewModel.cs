namespace ShopSphere.Models;

public sealed record OrderSummaryViewModel(
    int OrderId,
    string OrderNumber,
    DateTime OrderDate,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    OrderStatus OrderStatus);
