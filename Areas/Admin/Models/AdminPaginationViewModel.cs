using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Models;

public sealed record AdminPaginationViewModel(
    int Page,
    int TotalPages,
    string? Search = null,
    int? CategoryId = null,
    OrderStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null);
