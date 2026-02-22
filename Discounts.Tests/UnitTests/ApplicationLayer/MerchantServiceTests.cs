using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Discounts.Tests.UnitTests.Fixtures;
using FluentAssertions;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

public class MerchantServiceTests : IClassFixture<MerchantServiceTestsFixture>
{
    private readonly IMerchantService _sut;
    private readonly Mock<IDiscountRepository> _discountRepoMock;
    private readonly Mock<ICouponRepository> _couponRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepoMock;
    private readonly CancellationToken _testCt;

    public MerchantServiceTests(MerchantServiceTestsFixture fixture)
    {
        _sut = fixture.Sut;
        _discountRepoMock = fixture.DiscountRepoMock;
        _couponRepoMock = fixture.CouponRepoMock;
        _orderRepoMock = fixture.OrderRepoMock;
        _settingsRepoMock = fixture.SettingsRepoMock;
        _testCt = fixture.TestCt;

        _discountRepoMock.Invocations.Clear();
        _couponRepoMock.Invocations.Clear();
        _orderRepoMock.Invocations.Clear();
        _settingsRepoMock.Invocations.Clear();
        fixture.LocalizerMock.Invocations.Clear();
    }

    [Fact]
    public async Task GetDashboardAsync_WhenDiscountsExist_PopulatesAllFields()
    {
        var d1 = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Active, totalCoupons: 10, availableCoupons: 5);
        var d2 = MerchantServiceTestsFixture.CreateDiscount(2, "m1", DiscountStatus.Pending, totalCoupons: 20, availableCoupons: 20);
        var discounts = new List<Discount> { d1, d2 };

        _discountRepoMock
            .Setup(r => r.GetByMerchantIdAsync("m1", _testCt))
            .ReturnsAsync(discounts);

        var result = await _sut.GetDashboardAsync("m1", _testCt).ConfigureAwait(true);

        result.TotalDiscounts.Should().Be(2);
        result.ActiveDiscounts.Should().Be(1);
        result.PendingDiscounts.Should().Be(1);
        result.TotalSales.Should().Be(5); // d1 sold 10-5=5
        result.TotalRevenue.Should().Be(5 * 70m); // d1.DiscountedPrice * soldCoupons
        result.SalesStats.Should().ContainSingle();
        result.RecentDiscounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDiscountByIdAsync_WhenBelongsToMerchant_ReturnsModel()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1");
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.GetDiscountByIdAsync(1, "m1", _testCt).ConfigureAwait(true);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetDiscountByIdAsync_WhenBelongsToDifferentMerchant_ReturnsNull()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1");
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.GetDiscountByIdAsync(1, "other-merchant", _testCt).ConfigureAwait(true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateDiscountAsync_WhenValid_ReturnsCreatedModel()
    {
        var model = new CreateDiscountModel
        {
            Title = "New Discount",
            Description = "Desc",
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            TotalCoupons = 3,
            ValidFrom = DateTime.UtcNow.AddDays(1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            CategoryId = 1
        };

        _discountRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Discount>(), _testCt))
            .ReturnsAsync((Discount d, CancellationToken _) =>
            {
                d.Id = 42;
                return d;
            });

        _couponRepoMock
            .Setup(r => r.GenerateUniqueCouponCodeAsync(_testCt))
            .ReturnsAsync("CODE-123");

        _couponRepoMock
            .Setup(r => r.CreateBulkAsync(It.Is<IEnumerable<Coupon>>(c => c.Count() == 3), _testCt))
            .ReturnsAsync((IEnumerable<Coupon> c, CancellationToken _) => c.ToList());

        var result = await _sut.CreateDiscountAsync(model, "m1", _testCt).ConfigureAwait(true);

        result.Id.Should().Be(42);
        _discountRepoMock.Verify(r => r.CreateAsync(It.Is<Discount>(d => d.MerchantId == "m1"), _testCt), Times.Once);
        _couponRepoMock.Verify(r => r.GenerateUniqueCouponCodeAsync(_testCt), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateDiscountAsync_WhenDiscountedPriceNotLess_ThrowsInvalidOperationException()
    {
        var model = new CreateDiscountModel
        {
            OriginalPrice = 50m,
            DiscountedPrice = 50m, // equal
            ValidFrom = DateTime.UtcNow.AddDays(1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        Func<Task> act = () => _sut.CreateDiscountAsync(model, "m1", _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateDiscountAsync_WhenValidFromAfterValidTo_ThrowsInvalidOperationException()
    {
        var model = new CreateDiscountModel
        {
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            ValidFrom = DateTime.UtcNow.AddDays(30),
            ValidTo = DateTime.UtcNow.AddDays(1)
        };

        Func<Task> act = () => _sut.CreateDiscountAsync(model, "m1", _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateDiscountAsync_WhenStartDateInPast_ThrowsInvalidOperationException()
    {
        var model = new CreateDiscountModel
        {
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            ValidFrom = DateTime.UtcNow.AddDays(-5),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        Func<Task> act = () => _sut.CreateDiscountAsync(model, "m1", _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDiscountAsync_WhenValid_ReturnsUpdatedModel()
    {
        var existingDiscount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Pending);
        existingDiscount.CreatedAt = DateTime.UtcNow; // within edit window

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(existingDiscount);

        _settingsRepoMock
            .Setup(r => r.GetIntValueAsync(SettingsKeys.MerchantEditWindow, 24, _testCt))
            .ReturnsAsync(24);

        _discountRepoMock
            .Setup(r => r.UpdateAsync(existingDiscount, _testCt))
            .Returns(Task.CompletedTask);

        var model = new UpdateDiscountModel
        {
            Id = 1,
            Title = "Updated Title",
            Description = "Updated Desc",
            OriginalPrice = 200m,
            DiscountedPrice = 120m,
            ValidFrom = DateTime.UtcNow.AddDays(1),
            ValidTo = DateTime.UtcNow.AddDays(60),
            CategoryId = 2
        };

        var result = await _sut.UpdateDiscountAsync(model, "m1", _testCt).ConfigureAwait(true);

        result.Title.Should().Be("Updated Title");
        existingDiscount.CategoryId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateDiscountAsync_WhenWrongMerchant_ThrowsUnauthorizedAccessException()
    {
        var existingDiscount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Pending);

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(existingDiscount);

        var model = new UpdateDiscountModel
        {
            Id = 1,
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            ValidFrom = DateTime.UtcNow.AddDays(1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        Func<Task> act = () => _sut.UpdateDiscountAsync(model, "wrong-merchant", _testCt);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDiscountAsync_WhenEditWindowExpired_ThrowsInvalidOperationException()
    {
        var existingDiscount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Pending);
        existingDiscount.CreatedAt = DateTime.UtcNow.AddHours(-48); // outside 24h window

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(existingDiscount);

        _settingsRepoMock
            .Setup(r => r.GetIntValueAsync(SettingsKeys.MerchantEditWindow, 24, _testCt))
            .ReturnsAsync(24);

        var model = new UpdateDiscountModel
        {
            Id = 1,
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            ValidFrom = DateTime.UtcNow.AddDays(1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        Func<Task> act = () => _sut.UpdateDiscountAsync(model, "m1", _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task DeleteDiscountAsync_WhenValidAndNoSales_ReturnsTrue()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", totalCoupons: 10, availableCoupons: 10);

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        _discountRepoMock
            .Setup(r => r.DeleteAsync(1, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteDiscountAsync(1, "m1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDiscountAsync_WhenSoldCouponsExist_ThrowsInvalidOperationException()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", totalCoupons: 10, availableCoupons: 5); // 5 sold

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        Func<Task> act = () => _sut.DeleteDiscountAsync(1, "m1", _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task GetSalesHistoryAsync_WhenDiscountBelongsToMerchant_ReturnsMappedHistory()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1");
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var orders = new List<Order> { MerchantServiceTestsFixture.CreateOrder(1, 1), MerchantServiceTestsFixture.CreateOrder(2, 1) };
        _orderRepoMock
            .Setup(r => r.GetByDiscountIdAsync(1, _testCt))
            .ReturnsAsync(orders);

        var result = await _sut.GetSalesHistoryAsync(1, "m1", _testCt).ConfigureAwait(true);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CanEditDiscountAsync_WhenPendingAndWithinWindow_ReturnsTrue()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Pending);
        discount.CreatedAt = DateTime.UtcNow; // within 24h window

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        _settingsRepoMock
            .Setup(r => r.GetIntValueAsync(SettingsKeys.MerchantEditWindow, 24, _testCt))
            .ReturnsAsync(24);

        var result = await _sut.CanEditDiscountAsync(1, "m1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditDiscountAsync_WhenWrongMerchant_ReturnsFalse()
    {
        var discount = MerchantServiceTestsFixture.CreateDiscount(1, "m1", DiscountStatus.Pending);

        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.CanEditDiscountAsync(1, "wrong-merchant", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }
}
