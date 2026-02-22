using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Discounts.Tests.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

public class AuthServiceTests : IClassFixture<AuthServiceTestsFixture>
{
    private readonly IAuthService _sut;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly CancellationToken _testCt;

    public AuthServiceTests(AuthServiceTestsFixture fixture)
    {
        _sut = fixture.Sut;
        _userManagerMock = fixture.UserManagerMock;
        _testCt = fixture.TestCt;

        _userManagerMock.Invocations.Clear();
        fixture.LocalizerMock.Invocations.Clear();
    }

    [Fact]
    public async Task LoginAsync_WhenValidCredentials_ReturnsSuccessWithToken()
    {
        var user = AuthServiceTestsFixture.CreateUser();
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Customer });
        _userManagerMock
            .Setup(u => u.SetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new LoginRequestModel { Email = "test@test.com", Password = "Password1!" };

        var result = await _sut.LoginAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.UserId.Should().Be("user-1");
        result.Email.Should().Be("test@test.com");
        result.Roles.Should().Contain(Roles.Customer);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFailure()
    {
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequestModel { Email = "unknown@test.com", Password = "any" };

        var result = await _sut.LoginAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ReturnsFailure()
    {
        var user = AuthServiceTestsFixture.CreateUser();
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "wrong"))
            .ReturnsAsync(false);

        var request = new LoginRequestModel { Email = "test@test.com", Password = "wrong" };

        var result = await _sut.LoginAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountDeactivated_ReturnsFailure()
    {
        var user = AuthServiceTestsFixture.CreateUser(isActive: false);
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);

        var request = new LoginRequestModel { Email = "test@test.com", Password = "Password1!" };

        var result = await _sut.LoginAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_AccountDeactivated");
    }

    [Fact]
    public async Task RegisterAsync_WhenValidCustomer_ReturnsSuccessWithToken()
    {
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { Roles.Customer });
        _userManagerMock
            .Setup(u => u.SetAuthenticationTokenAsync(
                It.IsAny<ApplicationUser>(), "Discounts.API", "RefreshToken", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new RegisterRequestModel
        {
            Email = "new@test.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "Jane",
            LastName = "Smith",
            Role = Roles.Customer
        };

        var result = await _sut.RegisterAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("new@test.com");
        result.Roles.Should().Contain(Roles.Customer);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(AuthServiceTestsFixture.CreateUser());

        var request = new RegisterRequestModel
        {
            Email = "existing@test.com",
            Password = "Password1!",
            Role = Roles.Customer
        };

        var result = await _sut.RegisterAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_EmailAlreadyRegistered");
    }

    [Fact]
    public async Task RegisterAsync_WhenInvalidRole_ReturnsFailure()
    {
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new RegisterRequestModel
        {
            Email = "new@test.com",
            Password = "Password1!",
            Role = "InvalidRole"
        };

        var result = await _sut.RegisterAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_InvalidRole");
    }

    [Fact]
    public async Task RegisterAsync_WhenCreateFails_ReturnsErrorMessages()
    {
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too weak" }));

        var request = new RegisterRequestModel
        {
            Email = "new@test.com",
            Password = "weak",
            Role = Roles.Customer
        };

        var result = await _sut.RegisterAsync(request, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenValidToken_ReturnsNewTokenPair()
    {
        // Generate a real JWT so GetPrincipalFromExpiredToken can parse it
        var user = AuthServiceTestsFixture.CreateUser();
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Customer });
        _userManagerMock
            .Setup(u => u.SetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // First login to get a valid JWT
        var loginResult = await _sut.LoginAsync(
            new LoginRequestModel { Email = "test@test.com", Password = "Password1!" }, _testCt).ConfigureAwait(true);

        // Now set up for refresh
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.GetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken"))
            .ReturnsAsync(loginResult.RefreshToken);

        var refreshRequest = new RefreshTokenRequestModel
        {
            Token = loginResult.Token,
            RefreshToken = loginResult.RefreshToken
        };

        var result = await _sut.RefreshTokenAsync(refreshRequest, _testCt).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenStoredTokenDoesNotMatch_ReturnsFailure()
    {
        var user = AuthServiceTestsFixture.CreateUser();
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Customer });
        _userManagerMock
            .Setup(u => u.SetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var loginResult = await _sut.LoginAsync(
            new LoginRequestModel { Email = "test@test.com", Password = "Password1!" }, _testCt).ConfigureAwait(true);

        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.GetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken"))
            .ReturnsAsync("different-stored-token");

        var refreshRequest = new RefreshTokenRequestModel
        {
            Token = loginResult.Token,
            RefreshToken = "wrong-refresh-token"
        };

        var result = await _sut.RefreshTokenAsync(refreshRequest, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_InvalidRefreshToken");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenMalformedJwt_ThrowsSecurityTokenMalformedException()
    {
        var request = new RefreshTokenRequestModel
        {
            Token = "completely-invalid-jwt",
            RefreshToken = "any"
        };

        Func<Task> act = () => _sut.RefreshTokenAsync(request, _testCt);

        await act.Should().ThrowAsync<Microsoft.IdentityModel.Tokens.SecurityTokenMalformedException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserNotFound_ReturnsFailure()
    {
        var user = AuthServiceTestsFixture.CreateUser();
        _userManagerMock
            .Setup(u => u.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Customer });
        _userManagerMock
            .Setup(u => u.SetAuthenticationTokenAsync(user, "Discounts.API", "RefreshToken", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var loginResult = await _sut.LoginAsync(
            new LoginRequestModel { Email = "test@test.com", Password = "Password1!" }, _testCt).ConfigureAwait(true);

        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync((ApplicationUser?)null);

        var refreshRequest = new RefreshTokenRequestModel
        {
            Token = loginResult.Token,
            RefreshToken = loginResult.RefreshToken
        };

        var result = await _sut.RefreshTokenAsync(refreshRequest, _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_UserNotFound");
    }
}
