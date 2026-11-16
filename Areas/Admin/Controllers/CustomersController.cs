using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Areas.Admin.Models;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class CustomersController(
    ShopSphereDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const int PageSize = 20;
    private const int MaximumPage = 100_000;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        search = search?.Trim();
        if (search?.Length > 120 || page < 1 || page > MaximumPage)
        {
            return BadRequest();
        }

        var query = GetCustomerUsersQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.FullName.Contains(search) ||
                user.Email!.Contains(search) ||
                user.PhoneNumber!.Contains(search));
        }

        var totalCustomers = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCustomers / (double)PageSize));
        var currentPage = Math.Min(page, totalPages);
        var customers = await query
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(user => new AdminCustomerListItemViewModel(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return View(new CustomerManagementViewModel
        {
            Customers = customers,
            Search = search,
            Page = currentPage,
            TotalPages = totalPages,
            TotalCustomers = totalCustomers
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null || !await userManager.IsInRoleAsync(user, ApplicationRoles.Customer))
        {
            return NotFound();
        }

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == user.Id)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .Select(order => new OrderSummaryViewModel(
                order.OrderId,
                order.OrderNumber,
                order.OrderDate,
                order.TotalAmount,
                order.PaymentMethod,
                order.OrderStatus))
            .ToListAsync(cancellationToken);

        return View(new AdminCustomerDetailsViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            Orders = orders
        });
    }

    private IQueryable<ApplicationUser> GetCustomerUsersQuery() =>
        dbContext.Users.Where(user =>
            dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                dbContext.Roles.Any(role =>
                    role.Id == userRole.RoleId &&
                    role.Name == ApplicationRoles.Customer)));
}
