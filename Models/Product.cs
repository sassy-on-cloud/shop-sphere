using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class Product
{
    public int ProductId { get; set; }

    [Required]
    [StringLength(160)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercentage { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    [StringLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
