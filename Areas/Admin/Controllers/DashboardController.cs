using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Areas.Admin.Models;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class DashboardController(ShopSphereDbContext dbContext) : Controller
{
    private const int LowStockThreshold = 5;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customers = dbContext.Users.Where(user =>
            dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                dbContext.Roles.Any(role =>
                    role.Id == userRole.RoleId &&
                    role.Name == ApplicationRoles.Customer)));

        var viewModel = new AdminDashboardViewModel
        {
            TotalProducts = await dbContext.Products.CountAsync(cancellationToken),
            TotalCategories = await dbContext.Categories.CountAsync(cancellationToken),
            TotalCustomers = await customers.CountAsync(cancellationToken),
            TotalOrders = await dbContext.Orders.CountAsync(cancellationToken),
            DeliveredRevenue = await dbContext.Orders
                .Where(order => order.OrderStatus == OrderStatus.Delivered)
                .SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m,
            PendingOrders = await dbContext.Orders
                .CountAsync(order => order.OrderStatus == OrderStatus.Pending, cancellationToken),
            LowStockProducts = await dbContext.Products
                .CountAsync(product => product.StockQuantity <= LowStockThreshold, cancellationToken),
            RecentOrders = await dbContext.Orders
                .AsNoTracking()
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderId)
                .Take(6)
                .Select(order => new AdminOrderListItemViewModel(
                    order.OrderId,
                    order.OrderNumber,
                    order.CustomerName,
                    order.OrderDate,
                    order.TotalAmount,
                    order.OrderStatus))
                .ToListAsync(cancellationToken),
            RecentCustomers = await customers
                .AsNoTracking()
                .OrderByDescending(user => user.CreatedAt)
                .Take(6)
                .Select(user => new AdminCustomerListItemViewModel(
                    user.Id,
                    user.FullName,
                    user.Email ?? string.Empty,
                    user.CreatedAt))
                .ToListAsync(cancellationToken),
            LowStockAlerts = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.StockQuantity <= LowStockThreshold)
                .OrderBy(product => product.StockQuantity)
                .ThenBy(product => product.ProductName)
                .Take(8)
                .Select(product => new AdminLowStockProductViewModel(
                    product.ProductId,
                    product.ProductName,
                    product.StockQuantity))
                .ToListAsync(cancellationToken)
        };

        return View(viewModel);
    }
}
