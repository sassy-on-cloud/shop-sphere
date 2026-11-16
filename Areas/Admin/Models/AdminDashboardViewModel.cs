using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Models;

public sealed class AdminDashboardViewModel
{
    public int TotalProducts { get; init; }

    public int TotalCategories { get; init; }

    public int TotalCustomers { get; init; }

    public int TotalOrders { get; init; }

    public decimal DeliveredRevenue { get; init; }

    public int PendingOrders { get; init; }

    public int LowStockProducts { get; init; }

    public required IReadOnlyList<AdminOrderListItemViewModel> RecentOrders { get; init; }

    public required IReadOnlyList<AdminCustomerListItemViewModel> RecentCustomers { get; init; }

    public required IReadOnlyList<AdminLowStockProductViewModel> LowStockAlerts { get; init; }
}

public sealed record AdminOrderListItemViewModel(
    int OrderId,
    string OrderNumber,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    OrderStatus OrderStatus);

public sealed record AdminCustomerListItemViewModel(
    string UserId,
    string FullName,
    string Email,
    DateTime CreatedAt);

public sealed record AdminLowStockProductViewModel(
    int ProductId,
    string ProductName,
    int StockQuantity);
