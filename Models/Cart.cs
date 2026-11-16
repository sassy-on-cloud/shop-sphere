using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class Cart
{
    public int CartId { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(64)]
    public string? GuestCartKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
