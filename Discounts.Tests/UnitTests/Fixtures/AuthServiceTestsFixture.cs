using Discounts.Application;
using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Core;
using Discounts.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;

namespace Discounts.Tests.UnitTests.Fixtures;

public class AuthServiceTestsFixture
{
    public Mock<UserManager<ApplicationUser>> UserManagerMock { get; }
    public Mock<IStringLocalizer<ServiceMessages>> LocalizerMock { get; }
    public IConfiguration Configuration { get; }
    public IAuthService Sut { get; }
    public CancellationToken TestCt { get; }

    public AuthServiceTestsFixture()
    {
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        UserManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        LocalizerMock = new Mock<IStringLocalizer<ServiceMessages>>();

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposes1234567890!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationInMinutes"] = "60"
            })
            .Build();

        TestCt = new CancellationTokenSource().Token;

        LocalizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        Sut = new AuthService(
            UserManagerMock.Object,
            Configuration,
            LocalizerMock.Object);
    }

    public static ApplicationUser CreateUser(string id = "user-1", bool isActive = true) => new()
    {
        Id = id,
        UserName = "test@test.com",
        Email = "test@test.com",
        FirstName = "John",
        LastName = "Doe",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };
}
