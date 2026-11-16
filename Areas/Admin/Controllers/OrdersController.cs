using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Areas.Admin.Models;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class OrdersController(
    ShopSphereDbContext dbContext,
    OrderService orderService) : Controller
{
    private const int PageSize = 20;
    private const int MaximumPage = 100_000;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        search = search?.Trim();
        if (search?.Length > 120 ||
            page < 1 ||
            page > MaximumPage ||
            status.HasValue && !Enum.IsDefined(typeof(OrderStatus), status.Value) ||
            fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return BadRequest();
        }

        var query = dbContext.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(order => order.OrderNumber.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(order => order.OrderStatus == status.Value);
        }

        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            query = query.Where(order => order.OrderDate >= start);
        }

        if (toDate.HasValue)
        {
            var endDate = toDate.Value.Date;
            if (endDate == DateTime.MaxValue.Date)
            {
                query = query.Where(order => order.OrderDate <= DateTime.MaxValue);
            }
            else
            {
                var endExclusive = endDate.AddDays(1);
                query = query.Where(order => order.OrderDate < endExclusive);
            }
        }

        var totalOrders = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalOrders / (double)PageSize));
        var currentPage = Math.Min(page, totalPages);
        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(order => new AdminOrderListItemViewModel(
                order.OrderId,
                order.OrderNumber,
                order.CustomerName,
                order.OrderDate,
                order.TotalAmount,
                order.OrderStatus))
            .ToListAsync(cancellationToken);

        return View(new OrderManagementViewModel
        {
            Orders = orders,
            Search = search,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = currentPage,
            TotalPages = totalPages,
            TotalOrders = totalOrders
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.OrderId == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var customerEmail = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == order.UserId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);

        var details = new OrderDetailsViewModel
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            CustomerName = order.CustomerName,
            PhoneNumber = order.PhoneNumber,
            Province = order.Province,
            District = order.District,
            City = order.City,
            StreetAddress = order.StreetAddress,
            DeliveryInstructions = order.DeliveryInstructions,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            OrderStatus = order.OrderStatus,
            Items = order.Items
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderLineViewModel(
                    item.ProductName,
                    item.ProductImageUrl,
                    item.Quantity,
                    item.UnitPrice,
                    item.Subtotal))
                .ToList()
        };

        return View(new AdminOrderDetailsViewModel
        {
            Order = details,
            CustomerEmail = customerEmail ?? "Account unavailable",
            AllowedTransitions = OrderService.GetAllowedTransitions(order.OrderStatus)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await orderService.UpdateStatusAsync(id, status, cancellationToken);
        switch (result)
        {
            case OrderStatusUpdateStatus.Updated:
                TempData["AdminMessage"] = status == OrderStatus.Cancelled
                    ? "Order cancelled; reserved inventory has been restored."
                    : "Order status updated.";
                TempData["AdminMessageType"] = "success";
                break;
            case OrderStatusUpdateStatus.NotFound:
                return NotFound();
            default:
                TempData["AdminMessage"] = "That order status transition is not allowed.";
                TempData["AdminMessageType"] = "warning";
                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
