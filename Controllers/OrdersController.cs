using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Controllers;

[Authorize(Roles = ApplicationRoles.Customer)]
public sealed class OrdersController(
    UserManager<ApplicationUser> userManager,
    ShoppingCartService shoppingCartService,
    OrderService orderService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return await RenderCheckoutAsync(new CheckoutInput
        {
            SubmissionToken = Guid.NewGuid(),
            CustomerName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty
        }, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        [Bind(Prefix = "Input")] CheckoutInput input,
        CancellationToken cancellationToken)
    {
        if (input.SubmissionToken == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Refresh checkout and try again.");
        }

        if (!ModelState.IsValid)
        {
            return await RenderCheckoutAsync(input, cancellationToken);
        }

        var userId = ShoppingCartService.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var result = await orderService.PlaceOrderAsync(userId, input, cancellationToken);
        if (result.Status is OrderPlacementStatus.Created or OrderPlacementStatus.AlreadyPlaced)
        {
            return RedirectToAction(nameof(Confirmation), new { id = result.OrderId });
        }

        if (result.Status == OrderPlacementStatus.EmptyCart)
        {
            TempData["CartMessage"] = result.Message;
            TempData["CartMessageType"] = "warning";
            return RedirectToAction("Index", "Cart");
        }

        ModelState.AddModelError(string.Empty, result.Message ?? "We could not place the order. Please try again.");
        return await RenderCheckoutAsync(input, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = ShoppingCartService.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        return View(await orderService.GetOrdersAsync(userId, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await GetOrderDetailsAsync(id, cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
    {
        var order = await GetOrderDetailsAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var userId = ShoppingCartService.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var result = await orderService.CancelAsync(userId, id, cancellationToken);
        if (result == OrderCancellationStatus.NotFound)
        {
            return NotFound();
        }

        TempData["OrderMessage"] = result == OrderCancellationStatus.Cancelled
            ? "Your order was cancelled and the items were returned to stock."
            : "This order can no longer be cancelled.";
        TempData["OrderMessageType"] = result == OrderCancellationStatus.Cancelled ? "success" : "warning";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RenderCheckoutAsync(
        CheckoutInput input,
        CancellationToken cancellationToken)
    {
        var userId = ShoppingCartService.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var cart = await shoppingCartService.GetCartAsync(userId, null, cancellationToken);
        if (cart.Items.Count == 0)
        {
            TempData["CartMessage"] = "Your bag is empty.";
            TempData["CartMessageType"] = "warning";
            return RedirectToAction("Index", "Cart");
        }

        return View("Checkout", new CheckoutViewModel
        {
            Input = input,
            Cart = cart
        });
    }

    private async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var userId = ShoppingCartService.GetUserId(User);
        return userId is null
            ? null
            : await orderService.GetOrderDetailsAsync(userId, orderId, cancellationToken);
    }
}
