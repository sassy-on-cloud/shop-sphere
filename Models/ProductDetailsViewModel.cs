namespace ShopSphere.Models;

public sealed record ProductDetailsViewModel(
    Product Product,
    decimal CurrentPrice,
    IReadOnlyList<ProductCardViewModel> RelatedProducts);
