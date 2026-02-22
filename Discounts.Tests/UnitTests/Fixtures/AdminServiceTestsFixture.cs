using Discounts.Application;
using Discounts.Application.Interfaces;
using Discounts.Application.Mapping;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;
using Discounts.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;

namespace Discounts.Tests.UnitTests.Fixtures;

public class AdminServiceTestsFixture
{
    public Mock<IDiscountRepository> DiscountRepoMock { get; }
    public Mock<ICategoryRepository> CategoryRepoMock { get; }
    public Mock<ISystemSettingsRepository> SettingsRepoMock { get; }
    public Mock<IOrderRepository> OrderRepoMock { get; }
    public Mock<IStringLocalizer<ServiceMessages>> LocalizerMock { get; }
    public Mock<UserManager<ApplicationUser>> UserManagerMock { get; }
    public IAdminService Sut { get; }
    public CancellationToken TestCt { get; }

    static AdminServiceTestsFixture()
    {
        MappingConfig.Configure();
    }

    public AdminServiceTestsFixture()
    {
        DiscountRepoMock = new Mock<IDiscountRepository>(MockBehavior.Strict);
        CategoryRepoMock = new Mock<ICategoryRepository>(MockBehavior.Strict);
        SettingsRepoMock = new Mock<ISystemSettingsRepository>(MockBehavior.Strict);
        OrderRepoMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        LocalizerMock = new Mock<IStringLocalizer<ServiceMessages>>();

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        UserManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        TestCt = new CancellationTokenSource().Token;

        LocalizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        LocalizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns(static (string key, object[] args) =>
                new LocalizedString(key, value: string.Format(key, args)));

        Sut = new AdminService(
            UserManagerMock.Object,
            DiscountRepoMock.Object,
            CategoryRepoMock.Object,
            SettingsRepoMock.Object,
            OrderRepoMock.Object,
            LocalizerMock.Object);
    }

    public static Category CreateCategory(int id = 1, string name = "Food", bool isActive = true)
        => new()
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

    public static ApplicationUser CreateMerchant(string id = "merchant-1")
        => new()
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "TestCo",
            Email = "merchant@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

    public static Discount CreateDiscount(
        int id = 1,
        string merchantId = "merchant-1",
        DiscountStatus status = DiscountStatus.Active,
        int totalCoupons = 10,
        int availableCoupons = 5,
        int categoryId = 1)
    {
        var merchant = CreateMerchant(merchantId);
        var category = CreateCategory(categoryId);

        return new Discount
        {
            Id = id,
            Title = $"Discount #{id}",
            Description = "Test discount",
            OriginalPrice = 100m,
            DiscountedPrice = 70m,
            TotalCoupons = totalCoupons,
            AvailableCoupons = availableCoupons,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            Status = status,
            CategoryId = categoryId,
            Category = category,
            MerchantId = merchantId,
            Merchant = merchant,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }
}
