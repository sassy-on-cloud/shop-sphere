using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Models;

public sealed class CustomerManagementViewModel
{
    public required IReadOnlyList<AdminCustomerListItemViewModel> Customers { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalCustomers { get; init; }
}

public sealed class AdminCustomerDetailsViewModel
{
    public required string FullName { get; init; }

    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }

    public DateTime CreatedAt { get; init; }

    public required IReadOnlyList<OrderSummaryViewModel> Orders { get; init; }
}
