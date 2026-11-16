using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Areas.Admin.Models;

public sealed class CategoryManagementViewModel
{
    public required IReadOnlyList<CategoryManagementItemViewModel> Categories { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalCategories { get; init; }
}

public sealed record CategoryManagementItemViewModel(
    int CategoryId,
    string CategoryName,
    string? Description,
    string ImageUrl,
    int ProductCount);

public sealed class CategoryFormViewModel
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(2048)]
    [Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = string.Empty;
}

public sealed record CategoryDetailsViewModel(
    int CategoryId,
    string CategoryName,
    string? Description,
    string ImageUrl,
    int ProductCount);
