using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using ShopSphere.Areas.Admin.Controllers;
using ShopSphere.Models;
using ShopSphere.Models.Account;
using CustomerOrdersController = ShopSphere.Controllers.OrdersController;

namespace ShopSphere.Tests;

public sealed class AuthorizationAndValidationTests
{
    [Fact]
    public void AdminControllersRequireAdminRole()
    {
        var controllers = new[]
        {
            typeof(DashboardController),
            typeof(ProductsController),
            typeof(CategoriesController),
            typeof(OrdersController),
            typeof(CustomersController)
        };

        foreach (var controller in controllers)
        {
            var authorization = Assert.Single(controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true))
                as AuthorizeAttribute;
            Assert.Equal(ApplicationRoles.Admin, authorization!.Roles);
        }
    }

    [Fact]
    public void CustomerOrderControllerRequiresCustomerRole()
    {
        var authorization = Assert.Single(
            typeof(CustomerOrdersController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true))
            as AuthorizeAttribute;

        Assert.Equal(ApplicationRoles.Customer, authorization!.Roles);
    }

    [Fact]
    public void RegistrationValidationRejectsMismatchedPasswordsAndInvalidEmail()
    {
        var model = new RegisterViewModel
        {
            FullName = "Test Customer",
            Email = "not-an-email",
            PhoneNumber = "+9779800000000",
            Password = "Strong!Password123",
            ConfirmPassword = "Different!Password123"
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.Email)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.ConfirmPassword)));
    }
}
