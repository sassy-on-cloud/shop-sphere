using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, ShopSphereDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
    }

    public ShopSphereDbContext DbContext { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ShopSphereDbContext>()
            .UseSqlite(connection)
            .Options;
        var dbContext = new ShopSphereDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, dbContext);
    }

    public async Task<(ApplicationUser User, Product Product)> AddCustomerAndProductAsync(
        int stockQuantity = 10,
        decimal price = 1000m,
        decimal discountPercentage = 0m)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = "customer@example.test",
            NormalizedUserName = "CUSTOMER@EXAMPLE.TEST",
            Email = "customer@example.test",
            NormalizedEmail = "CUSTOMER@EXAMPLE.TEST",
            FullName = "Test Customer",
            CreatedAt = DateTime.UtcNow
        };
        var category = new Category
        {
            CategoryName = $"Test category {Guid.NewGuid():N}",
            Description = "Test category",
            ImageUrl = "/images/category.png"
        };
        var product = new Product
        {
            ProductName = $"Test product {Guid.NewGuid():N}",
            Description = "Product used only by automated tests.",
            Price = price,
            DiscountPercentage = discountPercentage,
            StockQuantity = stockQuantity,
            ImageUrl = "/images/product.png",
            Category = category,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return (user, product);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
