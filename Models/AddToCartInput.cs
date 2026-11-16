using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class AddToCartInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}
