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

internal sealed class MerchantControllerFixture
{
    public Mock<IMerchantService> MerchantService { get; }
    public Mock<IValidator<CreateDiscountDto>> CreateValidator { get; }
    public Mock<IValidator<UpdateDiscountDto>> UpdateValidator { get; }
    public MerchantController Sut { get; }

    public MerchantControllerFixture()
    {
        MapsterSetup.EnsureConfigured();

        MerchantService = new Mock<IMerchantService>(MockBehavior.Strict);
        CreateValidator = new Mock<IValidator<CreateDiscountDto>>(MockBehavior.Strict);
        UpdateValidator = new Mock<IValidator<UpdateDiscountDto>>(MockBehavior.Strict);

        Sut = new MerchantController(
            MerchantService.Object,
            CreateValidator.Object,
            UpdateValidator.Object);
    }

    public void SetAuthenticatedUser(string userId)
    {
        var user = ClaimsPrincipalHelper.CreateAuthenticatedUser(userId, "Merchant");
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
