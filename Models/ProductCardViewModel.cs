namespace ShopSphere.Models;

public sealed record ProductCardViewModel(
    int ProductId,
    string Name,
    string ImageUrl,
    decimal Price,
    decimal OriginalPrice,
    decimal DiscountPercent,
    string CategoryName,
    int StockQuantity,
    string Badge)
{
    public static ProductCardViewModel FromProduct(Product product, string badge)
    {
        var discountedPrice = decimal.Round(
            product.Price * (100m - product.DiscountPercentage) / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return new ProductCardViewModel(
            product.ProductId,
            product.ProductName,
            product.ImageUrl,
            discountedPrice,
            product.Price,
            product.DiscountPercentage,
            product.Category.CategoryName,
            product.StockQuantity,
            badge);
    }
}
