using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShopSphere.Controllers;

[Authorize]
public sealed class OrdersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
