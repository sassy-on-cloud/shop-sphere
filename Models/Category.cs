using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class Category
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(80)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
