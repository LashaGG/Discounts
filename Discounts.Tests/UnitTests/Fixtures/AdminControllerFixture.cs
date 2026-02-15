using Discounts.API.Controllers;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Tests.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers.Fixtures;

internal sealed class AdminControllerFixture
{
    public Mock<IAdminService> AdminService { get; }
    public Mock<IValidator<CreateCategoryDto>> CreateCategoryValidator { get; }
    public Mock<IValidator<UpdateCategoryDto>> UpdateCategoryValidator { get; }
    public AdminController Sut { get; }

    public AdminControllerFixture()
    {
        MapsterSetup.EnsureConfigured();

        AdminService = new Mock<IAdminService>(MockBehavior.Strict);
        CreateCategoryValidator = new Mock<IValidator<CreateCategoryDto>>(MockBehavior.Strict);
        UpdateCategoryValidator = new Mock<IValidator<UpdateCategoryDto>>(MockBehavior.Strict);

        Sut = new AdminController(
            AdminService.Object,
            CreateCategoryValidator.Object,
            UpdateCategoryValidator.Object);
    }

    public void SetAuthenticatedUser(string userId)
    {
        var user = ClaimsPrincipalHelper.CreateAuthenticatedUser(userId, "Administrator");
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void SetAnonymousUser()
    {
        var user = ClaimsPrincipalHelper.CreateAnonymousUser();
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public static ValidationResult ValidResult() => new();

    public static ValidationResult InvalidResult(string property, string message) =>
        new(new[] { new ValidationFailure(property, message) });
}
