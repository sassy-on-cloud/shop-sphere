namespace ShopSphere.Models;

public sealed class CheckoutViewModel
{
    public required CheckoutInput Input { get; init; }

    public required CartViewModel Cart { get; init; }
}
