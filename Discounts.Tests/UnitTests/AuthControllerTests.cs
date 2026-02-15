using Discounts.Application.DTOs;
using Discounts.Application.Models;
using Discounts.Tests.Controllers.Fixtures;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers;

public sealed class AuthControllerTests
{
    private readonly AuthControllerFixture _f;

    public AuthControllerTests()
    {
        _f = new AuthControllerFixture();
    }

    #region Login

    [Fact]
    public async Task Login_WhenSuccessful_ShouldReturnAuthResponse()
    {
        var request = new LoginRequestDto { Email = "user@test.com", Password = "Pass123!" };
        var model = new AuthResponseModel
        {
            Success = true,
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            Expiration = new DateTime(2024, 12, 31),
            UserId = "user-1",
            Email = "user@test.com",
            Roles = new List<string> { "Merchant" },
            Message = null
        };

        _f.LoginValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.Login(request, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Token.Should().Be("jwt-token");
        dto.RefreshToken.Should().Be("refresh-token");
        dto.Expiration.Should().Be(new DateTime(2024, 12, 31));
        dto.UserId.Should().Be("user-1");
        dto.Email.Should().Be("user@test.com");
        dto.Roles.Should().ContainSingle().Which.Should().Be("Merchant");
        dto.Message.Should().BeNull();
    }

    [Fact]
    public async Task Login_WhenFailed_ShouldReturnUnauthorized()
    {
        var request = new LoginRequestDto { Email = "user@test.com", Password = "wrong" };
        var model = new AuthResponseModel
        {
            Success = false,
            Message = "Invalid credentials"
        };

        _f.LoginValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.Login(request, CancellationToken.None).ConfigureAwait(true);

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problem = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(401);
        problem.Title.Should().Be("Authentication Failed");
        problem.Detail.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Login_WhenValidationFails_ShouldThrowValidationException()
    {
        var request = new LoginRequestDto { Email = "", Password = "" };

        _f.LoginValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.InvalidResult("Email", "Email is required"));

        var act = () => _f.Sut.Login(request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Login_WhenServiceThrows_ShouldPropagateException()
    {
        var request = new LoginRequestDto { Email = "x@x.com", Password = "p" };

        _f.LoginValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service down"));

        var act = () => _f.Sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Service down").ConfigureAwait(true);
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        var request = new RegisterRequestDto
        {
            Email = "new@test.com",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!",
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "TestCo",
            Role = "Merchant"
        };

        var model = new AuthResponseModel
        {
            Success = true,
            Token = "new-jwt",
            RefreshToken = "new-refresh",
            Expiration = new DateTime(2024, 12, 31),
            UserId = "new-user-1",
            Email = "new@test.com",
            Roles = new List<string> { "Merchant" },
            Message = null
        };

        _f.RegisterValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.Register(request, CancellationToken.None).ConfigureAwait(true);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_f.Sut.Login));

        var dto = created.Value.Should().BeOfType<AuthResponseDto>().Subject;
        dto.Success.Should().BeTrue();
        dto.Token.Should().Be("new-jwt");
        dto.RefreshToken.Should().Be("new-refresh");
        dto.UserId.Should().Be("new-user-1");
        dto.Email.Should().Be("new@test.com");
        dto.Roles.Should().ContainSingle().Which.Should().Be("Merchant");
    }

    [Fact]
    public async Task Register_WhenFailed_ShouldReturnBadRequest()
    {
        var request = new RegisterRequestDto
        {
            Email = "existing@test.com",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!",
            FirstName = "Jane",
            LastName = "Doe",
            Role = "Customer"
        };

        var model = new AuthResponseModel
        {
            Success = false,
            Message = "Email already exists"
        };

        _f.RegisterValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.Register(request, CancellationToken.None).ConfigureAwait(true);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Registration Failed");
        problem.Detail.Should().Be("Email already exists");
    }

    [Fact]
    public async Task Register_WhenValidationFails_ShouldThrowValidationException()
    {
        var request = new RegisterRequestDto { Email = "" };

        _f.RegisterValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.InvalidResult("Email", "Required"));

        var act = () => _f.Sut.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_WhenSuccessful_ShouldReturnNewTokens()
    {
        var request = new RefreshTokenRequestDto
        {
            Token = "old-jwt",
            RefreshToken = "old-refresh"
        };

        var model = new AuthResponseModel
        {
            Success = true,
            Token = "new-jwt",
            RefreshToken = "new-refresh",
            Expiration = new DateTime(2025, 1, 1),
            UserId = "user-1",
            Email = "user@test.com",
            Roles = new List<string> { "Customer" }
        };

        _f.RefreshValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.RefreshToken(request, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Token.Should().Be("new-jwt");
        dto.RefreshToken.Should().Be("new-refresh");
        dto.Expiration.Should().Be(new DateTime(2025, 1, 1));
        dto.UserId.Should().Be("user-1");
        dto.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task RefreshToken_WhenFailed_ShouldReturnUnauthorized()
    {
        var request = new RefreshTokenRequestDto
        {
            Token = "invalid",
            RefreshToken = "invalid"
        };

        var model = new AuthResponseModel
        {
            Success = false,
            Message = "Invalid refresh token"
        };

        _f.RefreshValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.ValidResult());

        _f.AuthService
            .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.RefreshToken(request, CancellationToken.None).ConfigureAwait(true);

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problem = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(401);
        problem.Title.Should().Be("Token Refresh Failed");
        problem.Detail.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshToken_WhenValidationFails_ShouldThrowValidationException()
    {
        var request = new RefreshTokenRequestDto { Token = "" };

        _f.RefreshValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthControllerFixture.InvalidResult("Token", "Required"));

        var act = () => _f.Sut.RefreshToken(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
    }

    #endregion
}
