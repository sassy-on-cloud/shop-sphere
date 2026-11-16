namespace ShopSphere.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded
}

public enum PaymentMethod
{
    CashOnDelivery
}
