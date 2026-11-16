using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShopSphere.Areas.Admin.Models;

public sealed class ProductManagementViewModel
{
    public required IReadOnlyList<ProductManagementItemViewModel> Products { get; init; }

    public required IReadOnlyList<SelectListItem> Categories { get; init; }

    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalProducts { get; init; }
}

public sealed record ProductManagementItemViewModel(
    int ProductId,
    string ProductName,
    string CategoryName,
    decimal Price,
    decimal DiscountPercentage,
    int StockQuantity,
    string ImageUrl);

public sealed class ProductFormViewModel
{
    [BindNever]
    public int ProductId { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Product name")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "100")]
    [Display(Name = "Discount percentage")]
    public decimal DiscountPercentage { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Stock quantity")]
    public int StockQuantity { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Product image")]
    public IFormFile? Image { get; set; }

    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [BindNever]
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
}
