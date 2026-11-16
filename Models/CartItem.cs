using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Cart Cart { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
