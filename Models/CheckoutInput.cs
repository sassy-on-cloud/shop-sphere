using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public sealed class CheckoutInput
{
    public Guid SubmissionToken { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Customer name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "City or municipality")]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [Display(Name = "Street address")]
    public string StreetAddress { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Additional delivery instructions")]
    public string? DeliveryInstructions { get; set; }
}
