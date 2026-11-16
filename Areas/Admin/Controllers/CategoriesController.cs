using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Areas.Admin.Models;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class CategoriesController(ShopSphereDbContext dbContext) : Controller
{
    private const int PageSize = 15;
    private const int MaximumPage = 100_000;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > MaximumPage)
        {
            return BadRequest();
        }

        var totalCategories = await dbContext.Categories.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCategories / (double)PageSize));
        var currentPage = Math.Min(page, totalPages);
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ThenBy(category => category.CategoryId)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(category => new CategoryManagementItemViewModel(
                category.CategoryId,
                category.CategoryName,
                category.Description,
                category.ImageUrl,
                category.Products.Count))
            .ToListAsync(cancellationToken);

        return View(new CategoryManagementViewModel
        {
            Categories = categories,
            Page = currentPage,
            TotalPages = totalPages,
            TotalCategories = totalCategories
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.CategoryId == id)
            .Select(item => new CategoryDetailsViewModel(
                item.CategoryId,
                item.CategoryName,
                item.Description,
                item.ImageUrl,
                item.Products.Count))
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : View(category);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateImageUrl(model.ImageUrl);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = new Category
        {
            CategoryName = model.CategoryName.Trim(),
            Description = NormalizeDescription(model.Description),
            ImageUrl = model.ImageUrl.Trim()
        };

        dbContext.Categories.Add(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
            return View(model);
        }

        TempData["AdminMessage"] = "Category created.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id = category.CategoryId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            ImageUrl = category.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.CategoryId = id;
        ValidateImageUrl(model.ImageUrl);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        category.CategoryName = model.CategoryName.Trim();
        category.Description = NormalizeDescription(model.Description);
        category.ImageUrl = model.ImageUrl.Trim();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
            return View(model);
        }

        TempData["AdminMessage"] = "Category updated.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (await dbContext.Products.AnyAsync(product => product.CategoryId == id, cancellationToken))
        {
            TempData["AdminMessage"] = "Reassign or remove the category's products before deleting it.";
            TempData["AdminMessageType"] = "warning";
            return RedirectToAction(nameof(Details), new { id });
        }

        dbContext.Categories.Remove(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsForeignKeyConflict(exception))
        {
            TempData["AdminMessage"] = "This category is in use and cannot be deleted.";
            TempData["AdminMessageType"] = "warning";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["AdminMessage"] = "Category deleted.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateImageUrl(string imageUrl)
    {
        var candidate = imageUrl.Trim();
        var validAbsoluteUrl = Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
                               uri.Scheme == Uri.UriSchemeHttps;
        var validAppPath = candidate.StartsWith("/", StringComparison.Ordinal) &&
                           !candidate.StartsWith("//", StringComparison.Ordinal) &&
                           !candidate.Contains('\\');
        if (!validAbsoluteUrl && !validAppPath)
        {
            ModelState.AddModelError(nameof(CategoryFormViewModel.ImageUrl),
                "Enter an HTTPS image URL or a path within the site.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static bool IsUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool IsForeignKeyConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 547 };
}
