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
        return new ProductCardViewModel(
            product.ProductId,
            product.ProductName,
            product.ImageUrl,
            ProductPricing.CalculateCurrentPrice(product),
            product.Price,
            product.DiscountPercentage,
            product.Category.CategoryName,
            product.StockQuantity,
            badge);
    }
}
