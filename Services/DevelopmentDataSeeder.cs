using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Services;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(ShopSphereDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var categories = new[]
        {
            new CategorySeed("Electronics", "Everyday tech and accessories.", "https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&w=320&q=80"),
            new CategorySeed("Fashion", "Comfortable staples and personal style.", "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=320&q=80"),
            new CategorySeed("Home & Living", "Small details that make a home.", "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=320&q=80"),
            new CategorySeed("Beauty", "Thoughtful care for every day.", "https://images.unsplash.com/photo-1596462502278-27bfdc403348?auto=format&fit=crop&w=320&q=80"),
            new CategorySeed("Sports", "Gear for moving, training, and exploring.", "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?auto=format&fit=crop&w=320&q=80"),
            new CategorySeed("Groceries", "Pantry essentials and local favorites.", "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=320&q=80")
        };

        var existingCategoryNames = (await dbContext.Categories
            .AsNoTracking()
            .Select(category => category.CategoryName)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            if (existingCategoryNames.Add(category.Name))
            {
                dbContext.Categories.Add(new Category
                {
                    CategoryName = category.Name,
                    Description = category.Description,
                    ImageUrl = category.ImageUrl
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryIds = await dbContext.Categories
            .AsNoTracking()
            .ToDictionaryAsync(category => category.CategoryName, category => category.CategoryId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var products = new[]
        {
            new ProductSeed("Studio wireless headphones", "Comfortable over-ear headphones with clear sound and a foldable design.", 3200m, 38m, 18, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=720&q=85", "Electronics"),
            new ProductSeed("Compact instant camera", "A simple point-and-shoot camera for capturing everyday moments.", 12500m, 35m, 7, "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=720&q=85", "Electronics"),
            new ProductSeed("Cloud-soft everyday sneakers", "Versatile cushioned sneakers made for comfortable all-day wear.", 3999m, 38m, 12, "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=720&q=85", "Fashion"),
            new ProductSeed("Everyday cotton crew tee", "A breathable cotton t-shirt with a relaxed, easy-to-style fit.", 1499m, 22m, 35, "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=720&q=85", "Fashion"),
            new ProductSeed("Minimal everyday wristwatch", "A clean analog watch with a comfortable strap and easy-read dial.", 5499m, 40m, 9, "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=720&q=85", "Fashion"),
            new ProductSeed("Lightweight city backpack", "A practical everyday backpack with room for a laptop and essentials.", 2599m, 20m, 21, "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?auto=format&fit=crop&w=720&q=85", "Fashion"),
            new ProductSeed("Soft knit home throw", "A soft textured throw that adds a warm layer to your living space.", 2799m, 18m, 16, "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=720&q=85", "Home & Living"),
            new ProductSeed("Ceramic accent vase", "A versatile ceramic vase with a simple finish for shelves and tables.", 1899m, 12m, 11, "https://images.unsplash.com/photo-1578500494198-246f612d3b3d?auto=format&fit=crop&w=720&q=85", "Home & Living"),
            new ProductSeed("Daily hydration bottle", "A reusable bottle with a leak-resistant lid for work and weekends.", 1099m, 20m, 28, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?auto=format&fit=crop&w=720&q=85", "Home & Living"),
            new ProductSeed("Fresh notes eau de parfum", "A bright, balanced fragrance suited to everyday wear.", 2299m, 25m, 14, "https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=720&q=85", "Beauty"),
            new ProductSeed("Daily hydration face serum", "A lightweight skin-care serum designed for a simple daily routine.", 1699m, 15m, 19, "https://images.unsplash.com/photo-1601049541289-9b1b7bbbfe19?auto=format&fit=crop&w=720&q=85", "Beauty"),
            new ProductSeed("Gentle cleansing set", "A convenient cleansing duo for a fresh, comfortable skin-care routine.", 1999m, 18m, 13, "https://images.unsplash.com/photo-1556229010-6c3f2c9ca5f8?auto=format&fit=crop&w=720&q=85", "Beauty"),
            new ProductSeed("Everyday running trainers", "Lightweight trainers with a grippy sole for runs and active days.", 3999m, 32m, 10, "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?auto=format&fit=crop&w=720&q=85", "Sports"),
            new ProductSeed("Training resistance band set", "A compact set of resistance bands for at-home strength sessions.", 1499m, 20m, 24, "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?auto=format&fit=crop&w=720&q=85", "Sports"),
            new ProductSeed("Trail-ready day pack", "A lightweight day pack with useful storage for short outdoor trips.", 3299m, 18m, 8, "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?auto=format&fit=crop&w=720&q=85", "Sports"),
            new ProductSeed("Nepal-grown roasted coffee", "Aromatic locally roasted coffee beans for a fresh cup at home.", 950m, 10m, 32, "https://images.unsplash.com/photo-1445116572660-236099ec97a0?auto=format&fit=crop&w=720&q=85", "Groceries"),
            new ProductSeed("Organic pantry honey", "A naturally sweet pantry staple for tea, toast, and home recipes.", 780m, 8m, 26, "https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=720&q=85", "Groceries"),
            new ProductSeed("Mixed nuts snack pack", "A satisfying mix of crunchy nuts for the desk, commute, or trail.", 650m, 5m, 40, "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=720&q=85", "Groceries")
        };

        var existingProductNames = (await dbContext.Products
            .AsNoTracking()
            .Select(product => product.ProductName)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            if (existingProductNames.Add(product.Name))
            {
                dbContext.Products.Add(new Product
                {
                    ProductName = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    DiscountPercentage = product.DiscountPercentage,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    CategoryId = categoryIds[product.CategoryName]
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record CategorySeed(string Name, string Description, string ImageUrl);

    private sealed record ProductSeed(
        string Name,
        string Description,
        decimal Price,
        decimal DiscountPercentage,
        int StockQuantity,
        string ImageUrl,
        string CategoryName);
}
