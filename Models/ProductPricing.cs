namespace ShopSphere.Models;

public static class ProductPricing
{
    public static decimal CalculateCurrentPrice(decimal price, decimal discountPercentage) =>
        decimal.Round(
            price * (100m - discountPercentage) / 100m,
            2,
            MidpointRounding.AwayFromZero);

    public static decimal CalculateCurrentPrice(Product product) =>
        CalculateCurrentPrice(product.Price, product.DiscountPercentage);
}
