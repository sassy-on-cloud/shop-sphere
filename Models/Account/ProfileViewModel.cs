using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models.Account;

public sealed class ProfileViewModel
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Account created")]
    public DateTime CreatedAt { get; set; }
}
