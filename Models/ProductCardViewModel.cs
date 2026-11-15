namespace ShopSphere.Models;

public sealed record ProductCardViewModel(
    string Name,
    string ImageUrl,
    decimal Price,
    decimal OriginalPrice,
    int DiscountPercent,
    double Rating,
    int ReviewCount,
    string Badge);
