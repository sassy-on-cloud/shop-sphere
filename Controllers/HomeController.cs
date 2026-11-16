using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers;

public class HomeController(ShopSphereDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var maximumDiscountPercentage = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.StockQuantity > 0)
            .Select(product => (decimal?)product.DiscountPercentage)
            .MaxAsync(cancellationToken) ?? 0m;

        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);

        var flashDeals = await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.StockQuantity > 0 && product.DiscountPercentage >= 30m)
            .OrderByDescending(product => product.DiscountPercentage)
            .ThenByDescending(product => product.CreatedAt)
            .Take(4)
            .ToListAsync(cancellationToken);

        var featuredProducts = await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.StockQuantity > 0 && product.DiscountPercentage < 30m)
            .OrderByDescending(product => product.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var viewModel = new HomeIndexViewModel
        {
            Categories = categories,
            FlashDeals = flashDeals
                .Select(product => ProductCardViewModel.FromProduct(product, "Flash deal"))
                .ToList(),
            FeaturedProducts = featuredProducts
                .Select(product => ProductCardViewModel.FromProduct(product, "Popular pick"))
                .ToList(),
            MaximumDiscountPercentage = maximumDiscountPercentage
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
