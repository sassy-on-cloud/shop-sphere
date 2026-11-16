using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Models;

public sealed class OrderManagementViewModel
{
    public required IReadOnlyList<AdminOrderListItemViewModel> Orders { get; init; }

    public string? Search { get; init; }

    public OrderStatus? Status { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalOrders { get; init; }
}

public sealed class AdminOrderDetailsViewModel
{
    public required OrderDetailsViewModel Order { get; init; }

    public required string CustomerEmail { get; init; }

    public required IReadOnlyList<OrderStatus> AllowedTransitions { get; init; }
}
