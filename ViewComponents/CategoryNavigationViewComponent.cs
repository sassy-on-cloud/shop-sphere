using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.ViewComponents;

public sealed class CategoryNavigationViewComponent(ShopSphereDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .Take(6)
            .Select(category => new CategoryNavigationItemViewModel(
                category.CategoryId,
                category.CategoryName))
            .ToListAsync(HttpContext.RequestAborted);

        return View(categories);
    }
}
