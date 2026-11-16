using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int? ProductId { get; set; }

    [Required]
    [StringLength(160)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(2048)]
    public string ProductImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Subtotal { get; set; }

    public Order Order { get; set; } = null!;

    public Product? Product { get; set; }
}
