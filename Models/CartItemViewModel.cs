namespace ShopSphere.Models;

public sealed record CartItemViewModel(
    int CartItemId,
    int ProductId,
    string ProductName,
    string ImageUrl,
    int Quantity,
    int StockQuantity,
    decimal UnitPrice,
    decimal Subtotal);
