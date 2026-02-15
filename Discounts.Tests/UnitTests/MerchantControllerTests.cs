using Discounts.Application.DTOs;
using Discounts.Application.Models;
using Discounts.Domain.Enums;
using Discounts.Tests.Controllers.Fixtures;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers;

public sealed class MerchantControllerTests
{
    private const string UserId = "merchant-42";
    private readonly MerchantControllerFixture _f;

    public MerchantControllerTests()
    {
        _f = new MerchantControllerFixture();
        _f.SetAuthenticatedUser(UserId);
    }

    #region GetDashboard

    [Fact]
    public async Task GetDashboard_WhenCalled_ShouldReturnMappedDashboard()
    {
        var model = new MerchantDashboardModel
        {
            TotalDiscounts = 10,
            ActiveDiscounts = 5,
            PendingDiscounts = 2,
            ExpiredDiscounts = 3,
            TotalSales = 100,
            TotalRevenue = 5000m
        };

        _f.MerchantService
            .Setup(s => s.GetDashboardAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetDashboard(CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.TotalDiscounts.Should().Be(10);
        dto.ActiveDiscounts.Should().Be(5);
        dto.PendingDiscounts.Should().Be(2);
        dto.ExpiredDiscounts.Should().Be(3);
        dto.TotalSales.Should().Be(100);
        dto.TotalRevenue.Should().Be(5000m);
    }

    [Fact]
    public async Task GetDashboard_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetDashboard(CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token").ConfigureAwait(true);
    }

    #endregion

    #region GetMyDiscounts

    [Fact]
    public async Task GetMyDiscounts_WhenStatusProvided_ShouldReturnFilteredDiscounts()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 1, Title = "D1", Status = DiscountStatus.Active }
        };

        _f.MerchantService
            .Setup(s => s.GetMerchantDiscountsByStatusAsync(UserId, DiscountStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyDiscounts(DiscountStatus.Active, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().NotBeNull();
        list.Should().HaveCount(1);
        list![0].Id.Should().Be(1);
        list[0].Title.Should().Be("D1");
        list[0].Status.Should().Be(DiscountStatus.Active);
    }

    [Fact]
    public async Task GetMyDiscounts_WhenStatusNull_ShouldReturnAllDiscounts()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 1, Title = "D1" },
            new() { Id = 2, Title = "D2" }
        };

        _f.MerchantService
            .Setup(s => s.GetMerchantDiscountsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetMyDiscounts(null, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().NotBeNull();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(1);
        list[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetMyDiscounts_WhenStatusProvidedAndEmpty_ShouldReturnEmptyList()
    {
        _f.MerchantService
            .Setup(s => s.GetMerchantDiscountsByStatusAsync(UserId, DiscountStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscountModel>());

        var result = await _f.Sut.GetMyDiscounts(DiscountStatus.Pending, CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDiscounts_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetMyDiscounts(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetDiscount

    [Fact]
    public async Task GetDiscount_WhenFound_ShouldReturnMappedDiscount()
    {
        var model = new DiscountModel
        {
            Id = 5,
            Title = "Half Off",
            Description = "50% off everything",
            OriginalPrice = 100m,
            DiscountedPrice = 50m,
            DiscountPercentage = 50m,
            TotalCoupons = 20,
            AvailableCoupons = 15,
            SoldCoupons = 5,
            ValidFrom = new DateTime(2024, 1, 1),
            ValidTo = new DateTime(2024, 12, 31),
            Status = DiscountStatus.Active,
            CategoryId = 3,
            CategoryName = "Electronics",
            MerchantId = UserId,
            MerchantName = "TestCo",
            CreatedAt = new DateTime(2024, 1, 1),
            IsActive = true,
            IsExpired = false,
            ImageUrl = "https://img.test/1.png"
        };

        _f.MerchantService
            .Setup(s => s.GetDiscountByIdAsync(5, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetDiscount(5, CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(5);
        dto.Title.Should().Be("Half Off");
        dto.Description.Should().Be("50% off everything");
        dto.OriginalPrice.Should().Be(100m);
        dto.DiscountedPrice.Should().Be(50m);
        dto.DiscountPercentage.Should().Be(50m);
        dto.TotalCoupons.Should().Be(20);
        dto.AvailableCoupons.Should().Be(15);
        dto.SoldCoupons.Should().Be(5);
        dto.ValidFrom.Should().Be(new DateTime(2024, 1, 1));
        dto.ValidTo.Should().Be(new DateTime(2024, 12, 31));
        dto.Status.Should().Be(DiscountStatus.Active);
        dto.CategoryId.Should().Be(3);
        dto.CategoryName.Should().Be("Electronics");
        dto.MerchantId.Should().Be(UserId);
        dto.MerchantName.Should().Be("TestCo");
        dto.CreatedAt.Should().Be(new DateTime(2024, 1, 1));
        dto.IsActive.Should().BeTrue();
        dto.IsExpired.Should().BeFalse();
        dto.ImageUrl.Should().Be("https://img.test/1.png");
    }

    [Fact]
    public async Task GetDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        _f.MerchantService
            .Setup(s => s.GetDiscountByIdAsync(99, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountModel?)null);

        var result = await _f.Sut.GetDiscount(99, CancellationToken.None).ConfigureAwait(true);

        result.Result.Should().BeOfType<NotFoundResult>();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetDiscount(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region CreateDiscount

    [Fact]
    public async Task CreateDiscount_WhenValid_ShouldReturnCreatedAtAction()
    {
        var dto = new CreateDiscountDto
        {
            Title = "New Deal",
            Description = "Great deal",
            OriginalPrice = 200m,
            DiscountedPrice = 100m,
            TotalCoupons = 50,
            ValidFrom = new DateTime(2024, 6, 1),
            ValidTo = new DateTime(2024, 12, 31),
            CategoryId = 1,
            ImageUrl = "https://img.test/new.png"
        };

        var createdModel = new DiscountModel
        {
            Id = 42,
            Title = "New Deal",
            Description = "Great deal",
            OriginalPrice = 200m,
            DiscountedPrice = 100m,
            TotalCoupons = 50,
            ValidFrom = new DateTime(2024, 6, 1),
            ValidTo = new DateTime(2024, 12, 31),
            CategoryId = 1,
            ImageUrl = "https://img.test/new.png"
        };

        _f.CreateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        _f.MerchantService
            .Setup(s => s.CreateDiscountAsync(It.IsAny<CreateDiscountModel>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdModel);

        var result = await _f.Sut.CreateDiscount(dto, CancellationToken.None).ConfigureAwait(true);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_f.Sut.GetDiscount));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(42);

        var returnedDto = created.Value.Should().BeOfType<DiscountDto>().Subject;
        returnedDto.Id.Should().Be(42);
        returnedDto.Title.Should().Be("New Deal");
        returnedDto.Description.Should().Be("Great deal");
        returnedDto.OriginalPrice.Should().Be(200m);
        returnedDto.DiscountedPrice.Should().Be(100m);
        returnedDto.TotalCoupons.Should().Be(50);
        returnedDto.CategoryId.Should().Be(1);
        returnedDto.ImageUrl.Should().Be("https://img.test/new.png");
    }

    [Fact]
    public async Task CreateDiscount_WhenValidationFails_ShouldThrowValidationException()
    {
        var dto = new CreateDiscountDto { Title = "" };

        _f.CreateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.InvalidResult("Title", "Title is required"));

        var act = () => _f.Sut.CreateDiscount(dto, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Title" && e.ErrorMessage == "Title is required");
    }

    [Fact]
    public async Task CreateDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var dto = new CreateDiscountDto { Title = "X" };
        _f.CreateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        var act = () => _f.Sut.CreateDiscount(dto, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateDiscount_WhenServiceThrows_ShouldPropagateException()
    {
        var dto = new CreateDiscountDto { Title = "X" };

        _f.CreateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        _f.MerchantService
            .Setup(s => s.CreateDiscountAsync(It.IsAny<CreateDiscountModel>(), UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate title"));

        var act = () => _f.Sut.CreateDiscount(dto, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Duplicate title").ConfigureAwait(true);
    }

    #endregion

    #region UpdateDiscount

    [Fact]
    public async Task UpdateDiscount_WhenValid_ShouldReturnUpdatedDiscount()
    {
        var dto = new UpdateDiscountDto
        {
            Title = "Updated",
            Description = "Upd desc",
            OriginalPrice = 300m,
            DiscountedPrice = 150m,
            ValidFrom = new DateTime(2024, 7, 1),
            ValidTo = new DateTime(2025, 1, 1),
            CategoryId = 2,
            ImageUrl = "https://img.test/upd.png"
        };

        var updatedModel = new DiscountModel
        {
            Id = 7,
            Title = "Updated",
            Description = "Upd desc",
            OriginalPrice = 300m,
            DiscountedPrice = 150m,
            ValidFrom = new DateTime(2024, 7, 1),
            ValidTo = new DateTime(2025, 1, 1),
            CategoryId = 2,
            ImageUrl = "https://img.test/upd.png"
        };

        _f.UpdateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        _f.MerchantService
            .Setup(s => s.UpdateDiscountAsync(It.IsAny<UpdateDiscountModel>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedModel);

        var result = await _f.Sut.UpdateDiscount(7, dto, CancellationToken.None).ConfigureAwait(true);

        dto.Id.Should().Be(7);

        var value = result.Value;
        value.Should().NotBeNull();
        value!.Id.Should().Be(7);
        value.Title.Should().Be("Updated");
        value.Description.Should().Be("Upd desc");
        value.OriginalPrice.Should().Be(300m);
        value.DiscountedPrice.Should().Be(150m);
        value.CategoryId.Should().Be(2);
        value.ImageUrl.Should().Be("https://img.test/upd.png");
    }

    [Fact]
    public async Task UpdateDiscount_ShouldSetIdFromRoute()
    {
        var dto = new UpdateDiscountDto { Id = 0, Title = "T" };

        _f.UpdateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        _f.MerchantService
            .Setup(s => s.UpdateDiscountAsync(It.IsAny<UpdateDiscountModel>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiscountModel { Id = 99 });

        await _f.Sut.UpdateDiscount(99, dto, CancellationToken.None).ConfigureAwait(true);

        dto.Id.Should().Be(99);
    }

    [Fact]
    public async Task UpdateDiscount_WhenValidationFails_ShouldThrowValidationException()
    {
        var dto = new UpdateDiscountDto { Title = "" };

        _f.UpdateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.InvalidResult("Title", "Required"));

        var act = () => _f.Sut.UpdateDiscount(1, dto, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var dto = new UpdateDiscountDto { Title = "X" };
        _f.UpdateValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MerchantControllerFixture.ValidResult());

        var act = () => _f.Sut.UpdateDiscount(1, dto, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region DeleteDiscount

    [Fact]
    public async Task DeleteDiscount_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.MerchantService
            .Setup(s => s.DeleteDiscountAsync(5, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.DeleteDiscount(5, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        _f.MerchantService
            .Setup(s => s.DeleteDiscountAsync(99, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.DeleteDiscount(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.DeleteDiscount(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetSalesHistory

    [Fact]
    public async Task GetSalesHistory_WhenCalled_ShouldReturnMappedHistory()
    {
        var models = new List<SalesHistoryModel>
        {
            new()
            {
                OrderId = 1,
                OrderNumber = "ORD-001",
                CustomerName = "John",
                CouponCount = 2,
                TotalAmount = 50m,
                OrderDate = new DateTime(2024, 3, 15),
                Status = "Completed"
            }
        };

        _f.MerchantService
            .Setup(s => s.GetSalesHistoryAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetSalesHistory(10, CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().NotBeNull();
        list.Should().HaveCount(1);
        list![0].OrderId.Should().Be(1);
        list[0].TotalAmount.Should().Be(50m);
    }

    [Fact]
    public async Task GetSalesHistory_WhenEmpty_ShouldReturnEmptyList()
    {
        _f.MerchantService
            .Setup(s => s.GetSalesHistoryAsync(10, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalesHistoryModel>());

        var result = await _f.Sut.GetSalesHistory(10, CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSalesHistory_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.GetSalesHistory(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region CanEdit

    [Fact]
    public async Task CanEdit_WhenTrue_ShouldReturnObjectWithCanEditTrue()
    {
        _f.MerchantService
            .Setup(s => s.CanEditDiscountAsync(5, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.CanEdit(5, CancellationToken.None).ConfigureAwait(true);

        var value = result.Value;
        value.Should().NotBeNull();

        var canEdit = value!.GetType().GetProperty("canEdit")!.GetValue(value);
        canEdit.Should().Be(true);
    }

    [Fact]
    public async Task CanEdit_WhenFalse_ShouldReturnObjectWithCanEditFalse()
    {
        _f.MerchantService
            .Setup(s => s.CanEditDiscountAsync(5, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.CanEdit(5, CancellationToken.None).ConfigureAwait(true);

        var value = result.Value;
        value.Should().NotBeNull();

        var canEdit = value!.GetType().GetProperty("canEdit")!.GetValue(value);
        canEdit.Should().Be(false);
    }

    [Fact]
    public async Task CanEdit_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.CanEdit(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion
}
