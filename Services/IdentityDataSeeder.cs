using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using ShopSphere.Models;

namespace ShopSphere.Services;

public static class IdentityDataSeeder
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        foreach (var roleName in new[] { ApplicationRoles.Customer, ApplicationRoles.Admin })
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to initialize the {roleName} role: {FormatIdentityErrors(result)}");
            }
        }

        var adminEmail = configuration["BootstrapAdmin:Email"];
        var adminPassword = configuration["BootstrapAdmin:Password"];
        var adminFullName = configuration["BootstrapAdmin:FullName"];

        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Bootstrap administrator setup requires both BootstrapAdmin:Email and BootstrapAdmin:Password.");
        }

        try
        {
            _ = new MailAddress(adminEmail);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("BootstrapAdmin:Email must be a valid email address.", exception);
        }

        if (adminPassword.Length < 12)
        {
            throw new InvalidOperationException("BootstrapAdmin:Password must contain at least 12 characters.");
        }

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is not null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, ApplicationRoles.Admin))
            {
                logger.LogWarning(
                    "Bootstrap administrator email matches an existing account. No role changes were made.");
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(adminFullName) ? "ShopSphere Administrator" : adminFullName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to create the configured bootstrap administrator: {FormatIdentityErrors(createResult)}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
        if (!roleResult.Succeeded)
        {
            var deleteResult = await userManager.DeleteAsync(admin);
            if (!deleteResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Administrator role assignment failed and the new account could not be removed: {FormatIdentityErrors(deleteResult)}");
            }

            throw new InvalidOperationException(
                $"Unable to assign the Admin role to the configured bootstrap administrator: {FormatIdentityErrors(roleResult)}");
        }

        logger.LogInformation("The configured bootstrap administrator was created.");
    }

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
