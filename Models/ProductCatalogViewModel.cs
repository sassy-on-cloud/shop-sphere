namespace ShopSphere.Models;

public sealed class ProductCatalogViewModel
{
    public required IReadOnlyList<ProductCardViewModel> Products { get; init; }

    public required IReadOnlyList<Category> Categories { get; init; }

    public string? SearchTerm { get; init; }

    public decimal? MinimumPrice { get; init; }

    public decimal? MaximumPrice { get; init; }

    public int? CategoryId { get; init; }

    public bool? Available { get; init; }

    public required string Sort { get; init; }

    public required int CurrentPage { get; init; }

    public required int TotalPages { get; init; }

    public required int TotalProducts { get; init; }

    public int PageSize { get; init; }

    public string? SelectedCategoryName =>
        Categories.FirstOrDefault(category => category.CategoryId == CategoryId)?.CategoryName;
}

public sealed class ProductCategoriesViewModel
{
    public required IReadOnlyList<Category> Categories { get; init; }
}
