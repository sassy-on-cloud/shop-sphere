using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ShopSphere.Models;

namespace ShopSphere.Data;

public sealed class ShopSphereDbContext(DbContextOptions<ShopSphereDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.CategoryId);
            entity.HasIndex(category => category.CategoryName).IsUnique();
            entity.Property(category => category.CategoryName).HasMaxLength(80).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500);
            entity.Property(category => category.ImageUrl).HasMaxLength(2048).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.ProductId);
            entity.HasIndex(product => product.CategoryId);
            entity.HasIndex(product => new { product.DiscountPercentage, product.StockQuantity });
            entity.HasIndex(product => product.CreatedAt);
            entity.Property(product => product.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(2000).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(product => product.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(product => product.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
