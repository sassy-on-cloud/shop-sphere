namespace ShopSphere.Models;

public sealed class CartViewModel
{
    public required IReadOnlyList<CartItemViewModel> Items { get; init; }

    public int TotalItems => Items.Sum(item => item.Quantity);

    public decimal TotalPrice => Items.Sum(item => item.Subtotal);
}
