using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;
using ShopSphere.Models.Account;
using ShopSphere.Services;

namespace ShopSphere.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ShoppingCartService shoppingCartService,
    GuestCartCookie guestCartCookie) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Customer);
        if (!roleResult.Succeeded)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Customer role assignment failed and the new account could not be removed: {FormatIdentityErrors(deleteResult)}");
            }

            ModelState.AddModelError(string.Empty, "We could not finish setting up your account. Please try again.");
            return View(model);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        await shoppingCartService.MergeGuestCartAsync(
            user.Id,
            guestCartCookie.GetToken(HttpContext),
            cancellationToken);
        guestCartCookie.Delete(HttpContext);
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(model.Email)
                ?? throw new InvalidOperationException("The signed-in account could not be loaded.");
            await shoppingCartService.MergeGuestCartAsync(
                user.Id,
                guestCartCookie.GetToken(HttpContext),
                HttpContext.RequestAborted);

            guestCartCookie.Delete(HttpContext);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email ?? string.Empty;
        model.CreatedAt = user.CreatedAt;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        TempData["ProfileMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
