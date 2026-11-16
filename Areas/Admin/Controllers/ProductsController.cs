using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Areas.Admin.Models;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class ProductsController(
    ShopSphereDbContext dbContext,
    ProductImageStorageService imageStorage) : Controller
{
    private const int PageSize = 15;
    private const int MaximumPage = 100_000;
    private const long MaximumRequestSize = ProductImageStorageService.MaximumFileSize + 1_048_576;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        search = search?.Trim();
        if (search?.Length > 160 || page < 1 || page > MaximumPage || categoryId is <= 0)
        {
            return BadRequest();
        }

        var categories = await GetCategoryOptionsAsync(cancellationToken);
        if (categoryId.HasValue && categories.All(item => item.Value != categoryId.Value.ToString()))
        {
            return NotFound();
        }

        var query = dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product =>
                product.ProductName.Contains(search) ||
                product.Description.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        var totalProducts = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalProducts / (double)PageSize));
        var currentPage = Math.Min(page, totalPages);
        var products = await query
            .OrderBy(product => product.ProductName)
            .ThenBy(product => product.ProductId)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(product => new ProductManagementItemViewModel(
                product.ProductId,
                product.ProductName,
                product.Category.CategoryName,
                product.Price,
                product.DiscountPercentage,
                product.StockQuantity,
                product.ImageUrl))
            .ToListAsync(cancellationToken);

        return View(new ProductManagementViewModel
        {
            Products = products,
            Categories = categories,
            Search = search,
            CategoryId = categoryId,
            Page = currentPage,
            TotalPages = totalPages,
            TotalProducts = totalProducts
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);

        return product is null ? NotFound() : View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await AddCategoriesAsync(new ProductFormViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestSize)]
    public async Task<IActionResult> Create(
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Image is null)
        {
            ModelState.AddModelError(nameof(model.Image), "Choose a product image.");
        }

        if (!ModelState.IsValid)
        {
            return View(await AddCategoriesAsync(model, cancellationToken));
        }

        if (!await dbContext.Categories.AnyAsync(
                category => category.CategoryId == model.CategoryId,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Select an existing category.");
            return View(await AddCategoriesAsync(model, cancellationToken));
        }

        string imageUrl;
        try
        {
            imageUrl = await imageStorage.SaveAsync(model.Image!, cancellationToken);
        }
        catch (ImageUploadValidationException exception)
        {
            ModelState.AddModelError(nameof(model.Image), exception.Message);
            return View(await AddCategoriesAsync(model, cancellationToken));
        }

        var product = new Product
        {
            ProductName = model.ProductName.Trim(),
            Description = model.Description.Trim(),
            Price = model.Price,
            DiscountPercentage = model.DiscountPercentage,
            StockQuantity = model.StockQuantity,
            CategoryId = model.CategoryId,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };

        var saved = false;
        try
        {
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);
            saved = true;
        }
        finally
        {
            if (!saved)
            {
                await imageStorage.DeleteManagedImageAsync(imageUrl);
            }
        }

        TempData["AdminMessage"] = "Product created.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id = product.ProductId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View(await AddCategoriesAsync(new ProductFormViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            DiscountPercentage = product.DiscountPercentage,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CurrentImageUrl = product.ImageUrl
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestSize)]
    public async Task<IActionResult> Edit(
        int id,
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        model.ProductId = product.ProductId;
        model.CurrentImageUrl = product.ImageUrl;
        if (!ModelState.IsValid)
        {
            return View(await AddCategoriesAsync(model, cancellationToken));
        }

        if (!await dbContext.Categories.AnyAsync(
                category => category.CategoryId == model.CategoryId,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Select an existing category.");
            return View(await AddCategoriesAsync(model, cancellationToken));
        }

        string? newImageUrl = null;
        if (model.Image is not null)
        {
            try
            {
                newImageUrl = await imageStorage.SaveAsync(model.Image, cancellationToken);
            }
            catch (ImageUploadValidationException exception)
            {
                ModelState.AddModelError(nameof(model.Image), exception.Message);
                return View(await AddCategoriesAsync(model, cancellationToken));
            }
        }

        var oldImageUrl = product.ImageUrl;
        product.ProductName = model.ProductName.Trim();
        product.Description = model.Description.Trim();
        product.Price = model.Price;
        product.DiscountPercentage = model.DiscountPercentage;
        product.StockQuantity = model.StockQuantity;
        product.CategoryId = model.CategoryId;
        if (newImageUrl is not null)
        {
            product.ImageUrl = newImageUrl;
        }

        var saved = false;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            saved = true;
        }
        finally
        {
            if (!saved && newImageUrl is not null)
            {
                await imageStorage.DeleteManagedImageAsync(newImageUrl);
            }
        }

        if (newImageUrl is not null)
        {
            await DeleteImageIfUnreferencedAsync(oldImageUrl, cancellationToken);
        }

        TempData["AdminMessage"] = "Product updated.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (await dbContext.CartItems.AnyAsync(item => item.ProductId == id, cancellationToken))
        {
            TempData["AdminMessage"] = "This product is in a customer bag and cannot be deleted yet.";
            TempData["AdminMessageType"] = "warning";
            return RedirectToAction(nameof(Details), new { id });
        }

        var oldImageUrl = product.ImageUrl;
        dbContext.Products.Remove(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsForeignKeyConflict(exception))
        {
            TempData["AdminMessage"] = "This product is currently referenced by another record and cannot be deleted.";
            TempData["AdminMessageType"] = "warning";
            return RedirectToAction(nameof(Details), new { id });
        }

        await DeleteImageIfUnreferencedAsync(oldImageUrl, cancellationToken);
        TempData["AdminMessage"] = "Product deleted.";
        TempData["AdminMessageType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductFormViewModel> AddCategoriesAsync(
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        return model;
    }

    private async Task DeleteImageIfUnreferencedAsync(
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }

        var referencedByProduct = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.ImageUrl == imageUrl, cancellationToken);
        var referencedByCategory = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(category => category.ImageUrl == imageUrl, cancellationToken);
        var referencedByOrder = await dbContext.OrderItems
            .AsNoTracking()
            .AnyAsync(item => item.ProductImageUrl == imageUrl, cancellationToken);

        if (!referencedByProduct && !referencedByCategory && !referencedByOrder)
        {
            await imageStorage.DeleteManagedImageAsync(imageUrl);
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .Select(category => new SelectListItem(category.CategoryName, category.CategoryId.ToString()))
            .ToListAsync(cancellationToken);
    }

    private static bool IsForeignKeyConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 547 };
}
