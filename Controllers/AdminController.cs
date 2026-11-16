using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;

namespace ShopSphere.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
