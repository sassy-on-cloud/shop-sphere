using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers;

public class ProductController(ShopSphereDbContext dbContext) : Controller
{
    private const int PageSize = 12;
    private const int MaximumPage = 100_000;
    private const decimal MaximumPrice = 9999999999999999.99m;
    private static readonly string[] SortOptions = ["newest", "price-asc", "price-desc", "name"];

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "q")] string? searchTerm,
        [FromQuery] decimal? minimumPrice,
        [FromQuery] decimal? maximumPrice,
        [FromQuery] int? categoryId,
        [FromQuery] bool? available,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        searchTerm = searchTerm?.Trim();
        if (searchTerm?.Length > 160)
        {
            ModelState.AddModelError("q", "Search terms must be 160 characters or fewer.");
        }

        if (minimumPrice is < 0 or > MaximumPrice)
        {
            ModelState.AddModelError(nameof(minimumPrice), $"Minimum price must be between 0 and {MaximumPrice:N2}.");
        }

        if (maximumPrice is < 0 or > MaximumPrice)
        {
            ModelState.AddModelError(nameof(maximumPrice), $"Maximum price must be between 0 and {MaximumPrice:N2}.");
        }

        if (minimumPrice.HasValue && maximumPrice.HasValue && minimumPrice > maximumPrice)
        {
            ModelState.AddModelError(nameof(maximumPrice), "Maximum price must be greater than or equal to minimum price.");
        }

        if (categoryId is <= 0)
        {
            ModelState.AddModelError(nameof(categoryId), "Select a valid category.");
        }

        if (page < 1 || page > MaximumPage)
        {
            ModelState.AddModelError(nameof(page), $"Page must be between 1 and {MaximumPage:N0}.");
        }

        if (!SortOptions.Contains(sort, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(sort), "Select a valid sort order.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);

        if (categoryId.HasValue && categories.All(category => category.CategoryId != categoryId.Value))
        {
            return NotFound();
        }

        IQueryable<Product> productsQuery = dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            productsQuery = productsQuery.Where(product =>
                product.ProductName.Contains(searchTerm) ||
                product.Description.Contains(searchTerm) ||
                product.Category.CategoryName.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.CategoryId == categoryId.Value);
        }

        if (available == true)
        {
            productsQuery = productsQuery.Where(product => product.StockQuantity > 0);
        }
        else if (available == false)
        {
            productsQuery = productsQuery.Where(product => product.StockQuantity == 0);
        }

        if (minimumPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product =>
                Math.Round(product.Price * (100m - product.DiscountPercentage) / 100m, 2) >= minimumPrice.Value);
        }

        if (maximumPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product =>
                Math.Round(product.Price * (100m - product.DiscountPercentage) / 100m, 2) <= maximumPrice.Value);
        }

        productsQuery = sort switch
        {
            "price-asc" => productsQuery
                .OrderBy(product => Math.Round(product.Price * (100m - product.DiscountPercentage) / 100m, 2))
                .ThenBy(product => product.ProductId),
            "price-desc" => productsQuery
                .OrderByDescending(product => Math.Round(product.Price * (100m - product.DiscountPercentage) / 100m, 2))
                .ThenBy(product => product.ProductId),
            "name" => productsQuery
                .OrderBy(product => product.ProductName)
                .ThenBy(product => product.ProductId),
            _ => productsQuery
                .OrderByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.ProductId)
        };

        var totalProducts = await productsQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalProducts / (double)PageSize));
        var currentPage = Math.Min(page, totalPages);

        var products = await productsQuery
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);

        var viewModel = new ProductCatalogViewModel
        {
            Products = products.Select(product => ProductCardViewModel.FromProduct(
                product,
                product.StockQuantity > 0 ? "In stock" : "Out of stock")).ToList(),
            Categories = categories,
            SearchTerm = searchTerm,
            MinimumPrice = minimumPrice,
            MaximumPrice = maximumPrice,
            CategoryId = categoryId,
            Available = available,
            Sort = sort,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalProducts = totalProducts,
            PageSize = PageSize
        };

        ViewData["Title"] = string.IsNullOrWhiteSpace(searchTerm)
            ? viewModel.SelectedCategoryName is null ? "Shop all products" : $"Shop {viewModel.SelectedCategoryName}"
            : $"Search: {searchTerm}";

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);

        return View(new ProductCategoriesViewModel { Categories = categories });
    }

    [HttpGet]
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

        var relatedProducts = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.CategoryId == product.CategoryId &&
                           item.ProductId != product.ProductId &&
                           item.StockQuantity > 0)
            .OrderByDescending(item => item.DiscountPercentage)
            .ThenByDescending(item => item.CreatedAt)
            .Take(4)
            .ToListAsync(cancellationToken);

        var currentPrice = ProductPricing.CalculateCurrentPrice(product);

        return View(new ProductDetailsViewModel(
            product,
            currentPrice,
            relatedProducts.Select(item => ProductCardViewModel.FromProduct(item, "In stock")).ToList()));
    }
}
