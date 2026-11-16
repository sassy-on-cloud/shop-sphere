using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class Order
{
    public int OrderId { get; set; }

    [Required]
    [StringLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string StreetAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DeliveryInstructions { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = global::ShopSphere.Models.PaymentMethod.CashOnDelivery;

    public PaymentStatus PaymentStatus { get; set; } = global::ShopSphere.Models.PaymentStatus.Pending;

    public OrderStatus OrderStatus { get; set; } = global::ShopSphere.Models.OrderStatus.Pending;

    public Guid SubmissionToken { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
