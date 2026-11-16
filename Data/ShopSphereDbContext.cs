using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ShopSphere.Models;

namespace ShopSphere.Data;

public sealed class ShopSphereDbContext(DbContextOptions<ShopSphereDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

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

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(cart => cart.CartId);
            entity.Property(cart => cart.UserId).HasMaxLength(450);
            entity.Property(cart => cart.GuestCartKey).HasMaxLength(64);
            entity.Property(cart => cart.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(cart => cart.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(cart => cart.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(cart => cart.GuestCartKey)
                .IsUnique()
                .HasFilter("[GuestCartKey] IS NOT NULL");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Carts_OneOwner",
                "([UserId] IS NOT NULL AND [GuestCartKey] IS NULL) OR ([UserId] IS NULL AND [GuestCartKey] IS NOT NULL)"));
            entity.HasOne(cart => cart.User)
                .WithMany(user => user.Carts)
                .HasForeignKey(cart => cart.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(item => item.CartItemId);
            entity.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
            entity.Property(item => item.AddedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.ToTable(table => table.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] > 0"));
            entity.HasOne(item => item.Cart)
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.CartItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.OrderId);
            entity.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
            entity.Property(order => order.UserId).HasMaxLength(450).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.CustomerName).HasMaxLength(120).IsRequired();
            entity.Property(order => order.PhoneNumber).HasMaxLength(32).IsRequired();
            entity.Property(order => order.Province).HasMaxLength(100).IsRequired();
            entity.Property(order => order.District).HasMaxLength(100).IsRequired();
            entity.Property(order => order.City).HasMaxLength(100).IsRequired();
            entity.Property(order => order.StreetAddress).HasMaxLength(300).IsRequired();
            entity.Property(order => order.DeliveryInstructions).HasMaxLength(500);
            entity.Property(order => order.PaymentMethod).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.PaymentStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.OrderStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.OrderDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.HasIndex(order => new { order.UserId, order.SubmissionToken }).IsUnique();
            entity.HasIndex(order => new { order.UserId, order.OrderDate });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Orders_OrderStatus",
                    "[OrderStatus] IN ('Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Cancelled')");
                table.HasCheckConstraint(
                    "CK_Orders_PaymentStatus",
                    "[PaymentStatus] IN ('Pending', 'Paid', 'Refunded')");
                table.HasCheckConstraint(
                    "CK_Orders_PaymentMethod",
                    "[PaymentMethod] = 'CashOnDelivery'");
            });
            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.OrderItemId);
            entity.Property(item => item.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ProductImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.Subtotal).HasPrecision(18, 2);
            entity.HasIndex(item => item.OrderId);
            entity.HasIndex(item => item.ProductId);
            entity.ToTable(table => table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0"));
            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
