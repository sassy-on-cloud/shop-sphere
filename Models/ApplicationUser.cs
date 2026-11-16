using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ShopSphere.Models;

public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
