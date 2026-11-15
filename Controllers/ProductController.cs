using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers;

public class ProductController(ShopSphereDbContext dbContext) : Controller
{
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ProductId == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var currentPrice = decimal.Round(
            product.Price * (100m - product.DiscountPercentage) / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return View(new ProductDetailsViewModel(product, currentPrice));
    }
}
