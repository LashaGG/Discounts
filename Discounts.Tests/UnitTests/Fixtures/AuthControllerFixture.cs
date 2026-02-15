using Discounts.API.Controllers;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Tests.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace Discounts.Tests.Controllers.Fixtures;

internal sealed class AuthControllerFixture
{
    public Mock<IAuthService> AuthService { get; }
    public Mock<IValidator<LoginRequestDto>> LoginValidator { get; }
    public Mock<IValidator<RegisterRequestDto>> RegisterValidator { get; }
    public Mock<IValidator<RefreshTokenRequestDto>> RefreshValidator { get; }
    public AuthController Sut { get; }

    public AuthControllerFixture()
    {
        MapsterSetup.EnsureConfigured();

        AuthService = new Mock<IAuthService>(MockBehavior.Strict);
        LoginValidator = new Mock<IValidator<LoginRequestDto>>(MockBehavior.Strict);
        RegisterValidator = new Mock<IValidator<RegisterRequestDto>>(MockBehavior.Strict);
        RefreshValidator = new Mock<IValidator<RefreshTokenRequestDto>>(MockBehavior.Strict);

        Sut = new AuthController(
            AuthService.Object,
            LoginValidator.Object,
            RegisterValidator.Object,
            RefreshValidator.Object);
    }

    public static ValidationResult ValidResult() => new();

    public static ValidationResult InvalidResult(string property, string message) =>
        new(new[] { new ValidationFailure(property, message) });
}
