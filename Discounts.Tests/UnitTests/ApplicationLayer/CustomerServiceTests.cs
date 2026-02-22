using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Discounts.Tests.UnitTests.Fixtures;
using FluentAssertions;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

public class CustomerServiceTests : IClassFixture<CustomerServiceTestsFixture>
{
    private readonly ICustomerService _sut;
    private readonly Mock<IDiscountRepository> _discountRepoMock;
    private readonly Mock<ICouponRepository> _couponRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepoMock;
    private readonly CancellationToken _testCt;

    public CustomerServiceTests(CustomerServiceTestsFixture fixture)
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
    public async Task GetActiveDiscountsAsync_WhenDiscountsExist_ReturnsMappedModels()
    {
        var discounts = new List<Discount>
        {
            CustomerServiceTestsFixture.CreateDiscount(1),
            CustomerServiceTestsFixture.CreateDiscount(2)
        };
        _discountRepoMock
            .Setup(r => r.GetActiveWithDetailsAsync(_testCt))
            .ReturnsAsync(discounts);

        var result = await _sut.GetActiveDiscountsAsync(_testCt).ConfigureAwait(true);

        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Discount #1");
        _discountRepoMock.Verify(r => r.GetActiveWithDetailsAsync(_testCt), Times.Once);
    }

    [Fact]
    public async Task SearchDiscountsAsync_WhenSearchTermIsWhitespace_FallsBackToActiveDiscounts()
    {
        var discounts = new List<Discount> { CustomerServiceTestsFixture.CreateDiscount() };
        _discountRepoMock
            .Setup(r => r.GetActiveWithDetailsAsync(_testCt))
            .ReturnsAsync(discounts);

        var result = await _sut.SearchDiscountsAsync("   ", _testCt).ConfigureAwait(true);

        result.Should().ContainSingle();
        _discountRepoMock.Verify(r => r.SearchActiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchDiscountsAsync_WhenValidTerm_CallsSearchActive()
    {
        var discounts = new List<Discount> { CustomerServiceTestsFixture.CreateDiscount() };
        _discountRepoMock
            .Setup(r => r.SearchActiveAsync("pizza", _testCt))
            .ReturnsAsync(discounts);

        var result = await _sut.SearchDiscountsAsync("pizza", _testCt).ConfigureAwait(true);

        result.Should().ContainSingle();
        _discountRepoMock.Verify(r => r.SearchActiveAsync("pizza", _testCt), Times.Once);
    }

    [Fact]
    public async Task ReserveCouponAsync_WhenDiscountNotFound_ReturnsFailure()
    {
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync((Discount?)null);

        var result = await _sut.ReserveCouponAsync(1, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_DiscountNotFound");
    }

    [Fact]
    public async Task ReserveCouponAsync_WhenDiscountNotActive_ReturnsFailure()
    {
        var discount = CustomerServiceTestsFixture.CreateDiscount(status: DiscountStatus.Pending);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(discount.Id, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.ReserveCouponAsync(discount.Id, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_DiscountNoLongerActive");
    }

    [Fact]
    public async Task ReserveCouponAsync_WhenNoCouponsAvailable_ReturnsFailure()
    {
        var discount = CustomerServiceTestsFixture.CreateDiscount(availableCoupons: 0);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(discount.Id, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.ReserveCouponAsync(discount.Id, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_CouponsSoldOut");
    }

    [Fact]
    public async Task ReserveCouponAsync_WhenAlreadyReservedAndNotExpired_ReturnsFailure()
    {
        var discount = CustomerServiceTestsFixture.CreateDiscount();
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(discount.Id, _testCt))
            .ReturnsAsync(discount);

        _settingsRepoMock
            .Setup(r => r.GetIntValueAsync(SettingsKeys.ReservationDuration, 30, _testCt))
            .ReturnsAsync(30);

        var existingCoupon = CustomerServiceTestsFixture.CreateCoupon(customerId: "cust-1", status: CouponStatus.Reserved);
        existingCoupon.ReservedAt = DateTime.UtcNow.AddMinutes(-5); // reserved 5 min ago, not expired
        _couponRepoMock
            .Setup(r => r.GetReservedByDiscountAndCustomerAsync(discount.Id, "cust-1", _testCt))
            .ReturnsAsync(existingCoupon);

        var result = await _sut.ReserveCouponAsync(discount.Id, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Service_AlreadyReserved");
    }

    [Fact]
    public async Task ReserveCouponAsync_WhenSuccessful_ReturnsSuccessWithCouponId()
    {
        var discount = CustomerServiceTestsFixture.CreateDiscount(availableCoupons: 3);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(discount.Id, _testCt))
            .ReturnsAsync(discount);

        _settingsRepoMock
            .Setup(r => r.GetIntValueAsync(SettingsKeys.ReservationDuration, 30, _testCt))
            .ReturnsAsync(15);

        _couponRepoMock
            .Setup(r => r.GetReservedByDiscountAndCustomerAsync(discount.Id, "cust-1", _testCt))
            .ReturnsAsync((Coupon?)null);

        var availableCoupon = CustomerServiceTestsFixture.CreateCoupon(id: 10);
        _couponRepoMock
            .Setup(r => r.GetFirstAvailableByDiscountAsync(discount.Id, _testCt))
            .ReturnsAsync(availableCoupon);

        _couponRepoMock
            .Setup(r => r.SaveChangesAsync(_testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.ReserveCouponAsync(discount.Id, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.CouponId.Should().Be(10);
        result.ReservationMinutes.Should().Be(15);
        availableCoupon.Status.Should().Be(CouponStatus.Reserved);
        availableCoupon.CustomerId.Should().Be("cust-1");
        discount.AvailableCoupons.Should().Be(2);
    }

    [Fact]
    public async Task PurchaseCouponAsync_WhenReservedCouponExists_CompletesPurchase()
    {
        var coupon = CustomerServiceTestsFixture.CreateCoupon(id: 5, customerId: "cust-1", status: CouponStatus.Reserved);
        coupon.Discount.DiscountedPrice = 50m;

        _couponRepoMock
            .Setup(r => r.GetReservedByDiscountAndCustomerWithDiscountAsync(1, "cust-1", _testCt))
            .ReturnsAsync(coupon);

        _orderRepoMock
            .Setup(r => r.GenerateOrderNumberAsync(_testCt))
            .ReturnsAsync("ORD-TEST-001");

        _orderRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), _testCt))
            .ReturnsAsync((Order o, CancellationToken _) => { o.Id = 100; return o; });

        _couponRepoMock
            .Setup(r => r.UpdateAsync(coupon, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.PurchaseCouponAsync(1, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.CouponId.Should().Be(5);
        result.Amount.Should().Be(50m);
        coupon.Status.Should().Be(CouponStatus.Purchased);
        coupon.PurchasedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PurchaseCouponAsync_WhenNoReservation_AndReserveFails_ReturnsFailure()
    {
        _couponRepoMock
            .Setup(r => r.GetReservedByDiscountAndCustomerWithDiscountAsync(1, "cust-1", _testCt))
            .ReturnsAsync((Coupon?)null);

        // ReserveCouponAsync will be called internally; make it fail
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync((Discount?)null);

        var result = await _sut.PurchaseCouponAsync(1, "cust-1", _testCt).ConfigureAwait(true);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CancelReservationAsync_WhenCouponFound_ReturnsTrue()
    {
        var coupon = CustomerServiceTestsFixture.CreateCoupon(id: 7, customerId: "cust-1", status: CouponStatus.Reserved);

        _couponRepoMock
            .Setup(r => r.GetReservedByIdAndCustomerWithDiscountAsync(7, "cust-1", _testCt))
            .ReturnsAsync(coupon);

        _couponRepoMock
            .Setup(r => r.SaveChangesAsync(_testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.CancelReservationAsync(7, "cust-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        coupon.Status.Should().Be(CouponStatus.Available);
        coupon.CustomerId.Should().BeNull();
        coupon.Discount.AvailableCoupons.Should().Be(6); // increased from 5
    }

    [Fact]
    public async Task GetMyActiveCouponsAsync_ReturnsMappedCoupons()
    {
        var coupons = new List<Coupon> { CustomerServiceTestsFixture.CreateCoupon(customerId: "cust-1", status: CouponStatus.Purchased) };
        _couponRepoMock
            .Setup(r => r.GetActivePurchasedByCustomerAsync("cust-1", _testCt))
            .ReturnsAsync(coupons);

        var result = await _sut.GetMyActiveCouponsAsync("cust-1", _testCt).ConfigureAwait(true);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task MarkCouponAsUsedAsync_WhenFound_ReturnsTrue()
    {
        var coupon = CustomerServiceTestsFixture.CreateCoupon(id: 4, customerId: "cust-1", status: CouponStatus.Purchased);
        _couponRepoMock
            .Setup(r => r.GetPurchasedByIdAndCustomerAsync(4, "cust-1", _testCt))
            .ReturnsAsync(coupon);

        _couponRepoMock
            .Setup(r => r.UpdateAsync(coupon, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.MarkCouponAsUsedAsync(4, "cust-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        coupon.Status.Should().Be(CouponStatus.Used);
        coupon.UsedAt.Should().NotBeNull();
    }

    }
