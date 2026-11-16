using Microsoft.AspNetCore.Mvc;
using ShopSphere.Services;

namespace ShopSphere.ViewComponents;

public sealed class CartCountViewComponent(
    ShoppingCartService shoppingCartService,
    GuestCartCookie guestCartCookie) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = ShoppingCartService.GetUserId(HttpContext.User);
        var count = await shoppingCartService.GetItemCountAsync(
            userId,
            userId is null ? guestCartCookie.GetToken(HttpContext) : null,
            HttpContext.RequestAborted);

        return View(count);
    }
}
