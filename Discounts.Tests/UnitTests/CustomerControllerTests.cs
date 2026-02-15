using Discounts.Application.DTOs;
using Discounts.Application.Models;
using Discounts.Domain.Enums;
using Discounts.Tests.Controllers.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers;

public sealed class CustomerControllerTests
{
    private const string UserId = "customer-42";
    private readonly CustomerControllerFixture _f;

    public CustomerControllerTests()
    {
        _f = new CustomerControllerFixture();
        _f.SetAuthenticatedUser(UserId);
    }

    #region GetActiveDiscounts

    [Fact]
    public async Task GetActiveDiscounts_WhenCalled_ShouldReturnMappedList()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 1, Title = "Deal1", Status = DiscountStatus.Active, IsActive = true },
            new() { Id = 2, Title = "Deal2", Status = DiscountStatus.Active, IsActive = true }
        };

        _f.CustomerService
            .Setup(s => s.GetActiveDiscountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetActiveDiscounts(CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().NotBeNull();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(1);
        list[0].Title.Should().Be("Deal1");
        list[1].Id.Should().Be(2);
        list[1].Title.Should().Be("Deal2");
    }

    [Fact]
    public async Task GetActiveDiscounts_WhenEmpty_ShouldReturnEmptyList()
    {
        _f.CustomerService
            .Setup(s => s.GetActiveDiscountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscountModel>());

        var result = await _f.Sut.GetActiveDiscounts(CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region GetByCategory

    [Fact]
    public async Task GetByCategory_WhenCalled_ShouldReturnFilteredDiscounts()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 3, Title = "Cat Deal", CategoryId = 5, CategoryName = "Food" }
        };

        _f.CustomerService
            .Setup(s => s.GetDiscountsByCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetByCategory(5, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(1);
        list![0].Id.Should().Be(3);
        list[0].CategoryId.Should().Be(5);
        list[0].CategoryName.Should().Be("Food");
    }

    [Fact]
    public async Task GetByCategory_WhenNoneFound_ShouldReturnEmptyList()
    {
        _f.CustomerService
            .Setup(s => s.GetDiscountsByCategoryAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscountModel>());

        var result = await _f.Sut.GetByCategory(999, CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Search

    [Fact]
    public async Task Search_WhenResultsExist_ShouldReturnMappedResults()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 10, Title = "Pizza Discount" }
        };

        _f.CustomerService
            .Setup(s => s.SearchDiscountsAsync("pizza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.Search("pizza", CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(1);
        list![0].Id.Should().Be(10);
        list[0].Title.Should().Be("Pizza Discount");
    }

    [Fact]
    public async Task Search_WhenNoResults_ShouldReturnEmptyList()
    {
        _f.CustomerService
            .Setup(s => s.SearchDiscountsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscountModel>());

        var result = await _f.Sut.Search("nonexistent", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Filter

    [Fact]
    public async Task Filter_WhenCalled_ShouldPassFilterAndReturnResults()
    {
        var filter = new DiscountFilterDto
        {
            CategoryId = 2,
            MinPrice = 10m,
            MaxPrice = 100m,
            MinDiscount = 5,
            MaxDiscount = 50,
            SearchTerm = "test",
            SortBy = "price",
            SortDescending = true
        };

        var models = new List<DiscountModel>
        {
            new() { Id = 20, Title = "Filtered", OriginalPrice = 50m, CategoryId = 2 }
        };

        _f.CustomerService
            .Setup(s => s.FilterDiscountsAsync(It.IsAny<DiscountFilterModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.Filter(filter, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(1);
        list![0].Id.Should().Be(20);
        list[0].Title.Should().Be("Filtered");
        list[0].CategoryId.Should().Be(2);
    }

    #endregion

    #region GetDiscountDetails

    [Fact]
    public async Task GetDiscountDetails_WhenFound_ShouldReturnMappedDiscount()
    {
        var model = new DiscountModel
        {
            Id = 15,
            Title = "Detail Deal",
            Description = "Detailed",
            OriginalPrice = 200m,
            DiscountedPrice = 150m,
            DiscountPercentage = 25m,
            TotalCoupons = 100,
            AvailableCoupons = 80,
            SoldCoupons = 20,
            ValidFrom = new DateTime(2024, 1, 1),
            ValidTo = new DateTime(2024, 6, 30),
            Status = DiscountStatus.Active,
            CategoryId = 3,
            CategoryName = "Tech",
            MerchantId = "m-1",
            MerchantName = "TechCo",
            CreatedAt = new DateTime(2024, 1, 1),
            IsActive = true,
            IsExpired = false,
            ImageUrl = "https://img.test/detail.png"
        };

        _f.CustomerService
            .Setup(s => s.GetDiscountDetailsAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetDiscountDetails(15, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(15);
        dto.Title.Should().Be("Detail Deal");
        dto.Description.Should().Be("Detailed");
        dto.OriginalPrice.Should().Be(200m);
        dto.DiscountedPrice.Should().Be(150m);
        dto.DiscountPercentage.Should().Be(25m);
        dto.TotalCoupons.Should().Be(100);
        dto.AvailableCoupons.Should().Be(80);
        dto.SoldCoupons.Should().Be(20);
        dto.CategoryId.Should().Be(3);
        dto.CategoryName.Should().Be("Tech");
        dto.MerchantId.Should().Be("m-1");
        dto.MerchantName.Should().Be("TechCo");
        dto.IsActive.Should().BeTrue();
        dto.IsExpired.Should().BeFalse();
        dto.ImageUrl.Should().Be("https://img.test/detail.png");
    }

    [Fact]
    public async Task GetDiscountDetails_WhenNotFound_ShouldReturnNotFound()
    {
        _f.CustomerService
            .Setup(s => s.GetDiscountDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountModel?)null);

        var result = await _f.Sut.GetDiscountDetails(999, CancellationToken.None).ConfigureAwait(true);

        result.Result.Should().BeOfType<NotFoundResult>();
        result.Value.Should().BeNull();
    }

    #endregion

    #region ReserveCoupon

    [Fact]
    public async Task ReserveCoupon_WhenSuccessful_ShouldReturnResult()
    {
        var model = new ReservationResultModel
        {
            Success = true,
            Message = "Reserved",
            CouponId = 42,
            ExpiresAt = new DateTime(2024, 6, 15, 12, 0, 0),
            ReservationMinutes = 30
        };

        _f.CustomerService
            .Setup(s => s.ReserveCouponAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.ReserveCoupon(10, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Message.Should().Be("Reserved");
        dto.CouponId.Should().Be(42);
        dto.ExpiresAt.Should().Be(new DateTime(2024, 6, 15, 12, 0, 0));
        dto.ReservationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task ReserveCoupon_WhenFailed_ShouldReturnBadRequest()
    {
        var model = new ReservationResultModel
        {
            Success = false,
            Message = "No coupons available",
            CouponId = null,
            ExpiresAt = null,
            ReservationMinutes = null
        };

        _f.CustomerService
            .Setup(s => s.ReserveCouponAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.ReserveCoupon(10, CancellationToken.None).ConfigureAwait(true);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Reservation Failed");
        problem.Detail.Should().Be("No coupons available");
    }

    [Fact]
    public async Task ReserveCoupon_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.ReserveCoupon(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region PurchaseCoupon

    [Fact]
    public async Task PurchaseCoupon_WhenSuccessful_ShouldReturnResult()
    {
        var model = new PurchaseResultModel
        {
            Success = true,
            Message = "Purchased",
            CouponId = 55,
            CouponCode = "CODE-123",
            Amount = 49.99m
        };

        _f.CustomerService
            .Setup(s => s.PurchaseCouponAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.PurchaseCoupon(10, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Message.Should().Be("Purchased");
        dto.CouponId.Should().Be(55);
        dto.CouponCode.Should().Be("CODE-123");
        dto.Amount.Should().Be(49.99m);
    }

    [Fact]
    public async Task PurchaseCoupon_WhenFailed_ShouldReturnBadRequest()
    {
        var model = new PurchaseResultModel
        {
            Success = false,
            Message = "Insufficient funds",
            CouponId = null,
            CouponCode = null,
            Amount = null
        };

        _f.CustomerService
            .Setup(s => s.PurchaseCouponAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.PurchaseCoupon(10, CancellationToken.None).ConfigureAwait(true);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Purchase Failed");
        problem.Detail.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task PurchaseCoupon_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.PurchaseCoupon(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region CancelReservation

    [Fact]
    public async Task CancelReservation_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.CustomerService
            .Setup(s => s.CancelReservationAsync(7, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.CancelReservation(7, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelReservation_WhenNotFound_ShouldReturnNotFound()
    {
        _f.CustomerService
            .Setup(s => s.CancelReservationAsync(99, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.CancelReservation(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CancelReservation_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.CancelReservation(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetMyCoupons

    [Fact]
    public async Task GetMyCoupons_WhenStatusNull_ShouldReturnAllCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 1, Code = "C1", Status = CouponStatus.Purchased },
            new() { Id = 2, Code = "C2", Status = CouponStatus.Used }
        };

        _f.CustomerService
            .Setup(s => s.GetAllMyCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons(null, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(1);
        list[0].Code.Should().Be("C1");
        list[1].Id.Should().Be(2);
        list[1].Code.Should().Be("C2");
    }

    [Fact]
    public async Task GetMyCoupons_WhenStatusActive_ShouldReturnActiveCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 3, Code = "C3", Status = CouponStatus.Purchased }
        };

        _f.CustomerService
            .Setup(s => s.GetMyActiveCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons("active", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
        result.Value!.ToList()[0].Code.Should().Be("C3");
    }

    [Fact]
    public async Task GetMyCoupons_WhenStatusUsed_ShouldReturnUsedCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 4, Code = "C4", Status = CouponStatus.Used }
        };

        _f.CustomerService
            .Setup(s => s.GetMyUsedCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons("used", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
        result.Value!.ToList()[0].Code.Should().Be("C4");
    }

    [Fact]
    public async Task GetMyCoupons_WhenStatusExpired_ShouldReturnExpiredCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 5, Code = "C5", Status = CouponStatus.Expired }
        };

        _f.CustomerService
            .Setup(s => s.GetMyExpiredCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons("expired", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
        result.Value!.ToList()[0].Code.Should().Be("C5");
    }

    [Fact]
    public async Task GetMyCoupons_WhenStatusUnknown_ShouldReturnAllCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 6, Code = "C6" }
        };

        _f.CustomerService
            .Setup(s => s.GetAllMyCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons("unknown", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyCoupons_WhenStatusActiveUpperCase_ShouldReturnActiveCoupons()
    {
        var models = new List<CouponModel>
        {
            new() { Id = 7, Code = "C7" }
        };

        _f.CustomerService
            .Setup(s => s.GetMyActiveCouponsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyCoupons("ACTIVE", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyCoupons_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetMyCoupons(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetCouponDetails

    [Fact]
    public async Task GetCouponDetails_WhenFound_ShouldReturnMappedCoupon()
    {
        var model = new CouponModel
        {
            Id = 10,
            Code = "COUP-10",
            DiscountId = 5,
            DiscountTitle = "Big Deal",
            DiscountImageUrl = "https://img.test/c.png",
            MerchantName = "ShopCo",
            CategoryName = "Food",
            OriginalPrice = 100m,
            DiscountedPrice = 70m,
            DiscountPercentage = 30,
            Status = CouponStatus.Purchased,
            ReservedAt = new DateTime(2024, 1, 1),
            PurchasedAt = new DateTime(2024, 1, 2),
            UsedAt = null,
            ValidFrom = new DateTime(2024, 1, 1),
            ValidTo = new DateTime(2025, 12, 31)
        };

        _f.CustomerService
            .Setup(s => s.GetCouponDetailsAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetCouponDetails(10, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(10);
        dto.Code.Should().Be("COUP-10");
        dto.DiscountId.Should().Be(5);
        dto.DiscountTitle.Should().Be("Big Deal");
        dto.DiscountImageUrl.Should().Be("https://img.test/c.png");
        dto.MerchantName.Should().Be("ShopCo");
        dto.CategoryName.Should().Be("Food");
        dto.OriginalPrice.Should().Be(100m);
        dto.DiscountedPrice.Should().Be(70m);
        dto.DiscountPercentage.Should().Be(30);
        dto.Status.Should().Be(CouponStatus.Purchased);
        dto.ReservedAt.Should().Be(new DateTime(2024, 1, 1));
        dto.PurchasedAt.Should().Be(new DateTime(2024, 1, 2));
        dto.UsedAt.Should().BeNull();
        dto.ValidFrom.Should().Be(new DateTime(2024, 1, 1));
        dto.ValidTo.Should().Be(new DateTime(2025, 12, 31));
    }

    [Fact]
    public async Task GetCouponDetails_WhenNotFound_ShouldReturnNotFound()
    {
        _f.CustomerService
            .Setup(s => s.GetCouponDetailsAsync(999, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CouponModel?)null);

        var result = await _f.Sut.GetCouponDetails(999, CancellationToken.None).ConfigureAwait(true);

        result.Result.Should().BeOfType<NotFoundResult>();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetCouponDetails_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetCouponDetails(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region MarkCouponAsUsed

    [Fact]
    public async Task MarkCouponAsUsed_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.CustomerService
            .Setup(s => s.MarkCouponAsUsedAsync(5, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.MarkCouponAsUsed(5, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkCouponAsUsed_WhenNotFound_ShouldReturnNotFound()
    {
        _f.CustomerService
            .Setup(s => s.MarkCouponAsUsedAsync(99, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.MarkCouponAsUsed(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkCouponAsUsed_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.MarkCouponAsUsed(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion
}
