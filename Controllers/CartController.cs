using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Controllers;

public class CartController(
    ShoppingCartService shoppingCartService,
    GuestCartCookie guestCartCookie) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cart = await shoppingCartService.GetCartAsync(
            ShoppingCartService.GetUserId(User),
            guestCartCookie.GetToken(HttpContext),
            cancellationToken);

        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        AddToCartInput input,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetMessage("Choose a valid product and quantity.", success: false);
            return RedirectToLocalOrProduct(returnUrl, input.ProductId);
        }

        var userId = ShoppingCartService.GetUserId(User);
        var guestToken = userId is null ? guestCartCookie.EnsureToken(HttpContext) : null;
        var result = await shoppingCartService.AddItemAsync(
            userId,
            guestToken,
            input.ProductId,
            input.Quantity,
            cancellationToken);

        SetMessage(result.Message, result.Succeeded);
        if (result.Status == CartOperationStatus.ProductNotFound)
        {
            return NotFound();
        }

        return RedirectToLocalOrProduct(returnUrl, input.ProductId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeQuantity(
        int cartItemId,
        int delta,
        CancellationToken cancellationToken)
    {
        var result = await shoppingCartService.ChangeQuantityAsync(
            ShoppingCartService.GetUserId(User),
            guestCartCookie.GetToken(HttpContext),
            cartItemId,
            delta,
            cancellationToken);

        SetMessage(result.Message, result.Succeeded);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId, CancellationToken cancellationToken)
    {
        var result = await shoppingCartService.RemoveItemAsync(
            ShoppingCartService.GetUserId(User),
            guestCartCookie.GetToken(HttpContext),
            cartItemId,
            cancellationToken);

        SetMessage(result.Message, result.Succeeded);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var result = await shoppingCartService.ClearAsync(
            ShoppingCartService.GetUserId(User),
            guestCartCookie.GetToken(HttpContext),
            cancellationToken);

        SetMessage(result.Message, result.Succeeded);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Checkout()
    {
        return View();
    }

    private IActionResult RedirectToLocalOrProduct(string? returnUrl, int productId)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return productId > 0
            ? RedirectToAction("Details", "Product", new { id = productId })
            : RedirectToAction("Index", "Product");
    }

    private void SetMessage(string message, bool success)
    {
        TempData["CartMessage"] = message;
        TempData["CartMessageType"] = success ? "success" : "warning";
    }
}
