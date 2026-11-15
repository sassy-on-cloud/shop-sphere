namespace ShopSphere.Models;

public sealed class HomeIndexViewModel
{
    public required IReadOnlyList<Category> Categories { get; init; }

    public required IReadOnlyList<ProductCardViewModel> FlashDeals { get; init; }

    public required IReadOnlyList<ProductCardViewModel> FeaturedProducts { get; init; }
}
