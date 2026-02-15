using Discounts.API.Requests;
using Discounts.Application.DTOs;
using Discounts.Application.Models;
using Discounts.Domain.Enums;
using Discounts.Tests.Controllers.Fixtures;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers;

public sealed class AdminControllerTests
{
    private const string AdminId = "admin-1";
    private readonly AdminControllerFixture _f;

    public AdminControllerTests()
    {
        _f = new AdminControllerFixture();
        _f.SetAuthenticatedUser(AdminId);
    }

    #region GetDashboard

    [Fact]
    public async Task GetDashboard_WhenCalled_ShouldReturnMappedDashboard()
    {
        var model = new AdminDashboardModel
        {
            TotalUsers = 50,
            TotalMerchants = 10,
            TotalCustomers = 35,
            TotalDiscounts = 100,
            PendingDiscounts = 5,
            ActiveDiscounts = 80,
            TotalOrders = 200,
            TotalRevenue = 25000m
        };

        _f.AdminService
            .Setup(s => s.GetDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetDashboard(CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.TotalUsers.Should().Be(50);
        dto.TotalMerchants.Should().Be(10);
        dto.TotalCustomers.Should().Be(35);
        dto.TotalDiscounts.Should().Be(100);
        dto.PendingDiscounts.Should().Be(5);
        dto.ActiveDiscounts.Should().Be(80);
        dto.TotalOrders.Should().Be(200);
        dto.TotalRevenue.Should().Be(25000m);
    }

    #endregion

    #region GetUsers

    [Fact]
    public async Task GetUsers_WhenRoleProvided_ShouldReturnFilteredUsers()
    {
        var models = new List<UserModel>
        {
            new() { Id = "u1", Email = "m@test.com", FirstName = "John", LastName = "Doe", IsActive = true }
        };

        _f.AdminService
            .Setup(s => s.GetUsersByRoleAsync("Merchant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetUsers("Merchant", CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(1);
        list![0].Id.Should().Be("u1");
        list[0].Email.Should().Be("m@test.com");
    }

    [Fact]
    public async Task GetUsers_WhenRoleNull_ShouldReturnAllUsers()
    {
        var models = new List<UserModel>
        {
            new() { Id = "u1", Email = "a@test.com" },
            new() { Id = "u2", Email = "b@test.com" }
        };

        _f.AdminService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetUsers(null, CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsers_WhenRoleEmpty_ShouldReturnAllUsers()
    {
        var models = new List<UserModel>
        {
            new() { Id = "u1", Email = "a@test.com" }
        };

        _f.AdminService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetUsers("", CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region GetUser

    [Fact]
    public async Task GetUser_WhenFound_ShouldReturnMappedUser()
    {
        var model = new UserModel
        {
            Id = "u5",
            Email = "user@test.com",
            FirstName = "Jane",
            LastName = "Smith",
            CompanyName = "SmithCo",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1),
            Roles = new List<string> { "Customer" }
        };

        _f.AdminService
            .Setup(s => s.GetUserByIdAsync("u5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _f.Sut.GetUser("u5", CancellationToken.None).ConfigureAwait(true);

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.Id.Should().Be("u5");
        dto.Email.Should().Be("user@test.com");
        dto.FirstName.Should().Be("Jane");
        dto.LastName.Should().Be("Smith");
        dto.CompanyName.Should().Be("SmithCo");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(new DateTime(2024, 1, 1));
        dto.Roles.Should().ContainSingle().Which.Should().Be("Customer");
    }

    [Fact]
    public async Task GetUser_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.GetUserByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserModel?)null);

        var result = await _f.Sut.GetUser("missing", CancellationToken.None).ConfigureAwait(true);

        result.Result.Should().BeOfType<NotFoundResult>();
        result.Value.Should().BeNull();
    }

    #endregion

    #region DeactivateUser

    [Fact]
    public async Task DeactivateUser_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.DeactivateUserAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.DeactivateUser("u1", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeactivateUser_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.DeactivateUserAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.DeactivateUser("missing", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ActivateUser

    [Fact]
    public async Task ActivateUser_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.ActivateUserAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.ActivateUser("u1", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ActivateUser_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.ActivateUserAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.ActivateUser("missing", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteUser

    [Fact]
    public async Task DeleteUser_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.DeleteUserAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.DeleteUser("u1", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.DeleteUserAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.DeleteUser("missing", CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region UpdateUserRole

    [Fact]
    public async Task UpdateUserRole_WhenSuccessful_ShouldReturnNoContent()
    {
        var request = new UpdateRoleRequest { Role = "Merchant" };

        _f.AdminService
            .Setup(s => s.UpdateUserRoleAsync("u1", "Merchant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.UpdateUserRole("u1", request, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateUserRole_WhenNotFound_ShouldReturnNotFound()
    {
        var request = new UpdateRoleRequest { Role = "Admin" };

        _f.AdminService
            .Setup(s => s.UpdateUserRoleAsync("missing", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.UpdateUserRole("missing", request, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetPendingDiscounts

    [Fact]
    public async Task GetPendingDiscounts_WhenCalled_ShouldReturnMappedList()
    {
        var models = new List<DiscountModel>
        {
            new() { Id = 1, Title = "Pending1", Status = DiscountStatus.Pending },
            new() { Id = 2, Title = "Pending2", Status = DiscountStatus.Pending }
        };

        _f.AdminService
            .Setup(s => s.GetPendingDiscountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetPendingDiscounts(CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(1);
        list[0].Status.Should().Be(DiscountStatus.Pending);
        list[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingDiscounts_WhenEmpty_ShouldReturnEmptyList()
    {
        _f.AdminService
            .Setup(s => s.GetPendingDiscountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscountModel>());

        var result = await _f.Sut.GetPendingDiscounts(CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().BeEmpty();
    }

    #endregion

    #region ApproveDiscount

    [Fact]
    public async Task ApproveDiscount_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.ApproveDiscountAsync(1, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.ApproveDiscount(1, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ApproveDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.ApproveDiscountAsync(99, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.ApproveDiscount(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ApproveDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.ApproveDiscount(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region RejectDiscount

    [Fact]
    public async Task RejectDiscount_WhenSuccessful_ShouldReturnNoContent()
    {
        var request = new RejectDiscountRequest { Reason = "Low quality" };

        _f.AdminService
            .Setup(s => s.RejectDiscountAsync(1, AdminId, "Low quality", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.RejectDiscount(1, request, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RejectDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        var request = new RejectDiscountRequest { Reason = "Bad" };

        _f.AdminService
            .Setup(s => s.RejectDiscountAsync(99, AdminId, "Bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.RejectDiscount(99, request, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RejectDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.RejectDiscount(1, new RejectDiscountRequest { Reason = "x" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region SuspendDiscount

    [Fact]
    public async Task SuspendDiscount_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.SuspendDiscountAsync(1, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.SuspendDiscount(1, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SuspendDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.SuspendDiscountAsync(99, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.SuspendDiscount(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SuspendDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.SuspendDiscount(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region ActivateDiscount

    [Fact]
    public async Task ActivateDiscount_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.ActivateDiscountAsync(1, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.ActivateDiscount(1, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ActivateDiscount_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.ActivateDiscountAsync(99, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.ActivateDiscount(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ActivateDiscount_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.ActivateDiscount(1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetSettings

    [Fact]
    public async Task GetSettings_WhenCalled_ShouldReturnSettings()
    {
        var settings = new Dictionary<string, string>
        {
            ["MaxCoupons"] = "100",
            ["ReservationMinutes"] = "30"
        };

        _f.AdminService
            .Setup(s => s.GetAllSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await _f.Sut.GetSettings(CancellationToken.None).ConfigureAwait(true);

        var value = result.Value;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value!["MaxCoupons"].Should().Be("100");
        value["ReservationMinutes"].Should().Be("30");
    }

    [Fact]
    public async Task GetSettings_WhenEmpty_ShouldReturnEmptyDictionary()
    {
        _f.AdminService
            .Setup(s => s.GetAllSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _f.Sut.GetSettings(CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().BeEmpty();
    }

    #endregion

    #region UpdateSetting

    [Fact]
    public async Task UpdateSetting_WhenCalled_ShouldReturnNoContent()
    {
        var request = new UpdateSettingRequest { Value = "200" };

        _f.AdminService
            .Setup(s => s.UpdateSettingAsync("MaxCoupons", "200", AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.UpdateSetting("MaxCoupons", request, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateSetting_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.UpdateSetting("key", new UpdateSettingRequest { Value = "v" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region UpdateMultipleSettings

    [Fact]
    public async Task UpdateMultipleSettings_WhenCalled_ShouldReturnNoContent()
    {
        var settings = new Dictionary<string, string>
        {
            ["Key1"] = "Val1",
            ["Key2"] = "Val2"
        };

        _f.AdminService
            .Setup(s => s.UpdateMultipleSettingsAsync(settings, AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.UpdateMultipleSettings(settings, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateMultipleSettings_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _f.SetAnonymousUser();

        var act = () => _f.Sut.UpdateMultipleSettings(new Dictionary<string, string>(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().ConfigureAwait(true);
    }

    #endregion

    #region GetCategories

    [Fact]
    public async Task GetCategories_WhenCalled_ShouldReturnMappedCategories()
    {
        var models = new List<CategoryModel>
        {
            new() { Id = 1, Name = "Food", Description = "Food items", IconClass = "fa-food", IsActive = true, CreatedAt = new DateTime(2024, 1, 1), DiscountsCount = 5 },
            new() { Id = 2, Name = "Tech", Description = null, IconClass = null, IsActive = false, CreatedAt = new DateTime(2024, 2, 1), DiscountsCount = 0 }
        };

        _f.AdminService
            .Setup(s => s.GetAllCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetCategories(CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(1);
        list[0].Name.Should().Be("Food");
        list[0].Description.Should().Be("Food items");
        list[0].IconClass.Should().Be("fa-food");
        list[0].IsActive.Should().BeTrue();
        list[0].DiscountsCount.Should().Be(5);
        list[1].Id.Should().Be(2);
        list[1].Name.Should().Be("Tech");
        list[1].Description.Should().BeNull();
        list[1].IsActive.Should().BeFalse();
        list[1].DiscountsCount.Should().Be(0);
    }

    #endregion

    #region CreateCategory

    [Fact]
    public async Task CreateCategory_WhenValid_ShouldReturnCreatedAtAction()
    {
        var dto = new CreateCategoryDto
        {
            Name = "NewCat",
            Description = "New Category",
            IconClass = "fa-new"
        };

        var createdModel = new CategoryModel
        {
            Id = 10,
            Name = "NewCat",
            Description = "New Category",
            IconClass = "fa-new",
            IsActive = true,
            CreatedAt = new DateTime(2024, 6, 1),
            DiscountsCount = 0
        };

        _f.CreateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.ValidResult());

        _f.AdminService
            .Setup(s => s.CreateCategoryAsync(It.IsAny<CreateCategoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdModel);

        var result = await _f.Sut.CreateCategory(dto, CancellationToken.None).ConfigureAwait(true);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_f.Sut.GetCategories));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(10);

        var returnedDto = created.Value.Should().BeOfType<CategoryDto>().Subject;
        returnedDto.Id.Should().Be(10);
        returnedDto.Name.Should().Be("NewCat");
        returnedDto.Description.Should().Be("New Category");
        returnedDto.IconClass.Should().Be("fa-new");
        returnedDto.IsActive.Should().BeTrue();
        returnedDto.DiscountsCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateCategory_WhenValidationFails_ShouldThrowValidationException()
    {
        var dto = new CreateCategoryDto { Name = "" };

        _f.CreateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.InvalidResult("Name", "Name is required"));

        var act = () => _f.Sut.CreateCategory(dto, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    #endregion

    #region UpdateCategory

    [Fact]
    public async Task UpdateCategory_WhenSuccessful_ShouldReturnNoContent()
    {
        var dto = new UpdateCategoryDto
        {
            Name = "Updated",
            Description = "Updated desc",
            IconClass = "fa-upd",
            IsActive = true
        };

        _f.UpdateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.ValidResult());

        _f.AdminService
            .Setup(s => s.UpdateCategoryAsync(It.IsAny<UpdateCategoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.UpdateCategory(5, dto, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
        dto.Id.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCategory_WhenNotFound_ShouldReturnNotFound()
    {
        var dto = new UpdateCategoryDto { Name = "X" };

        _f.UpdateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.ValidResult());

        _f.AdminService
            .Setup(s => s.UpdateCategoryAsync(It.IsAny<UpdateCategoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.UpdateCategory(99, dto, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateCategory_ShouldSetIdFromRoute()
    {
        var dto = new UpdateCategoryDto { Id = 0, Name = "T" };

        _f.UpdateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.ValidResult());

        _f.AdminService
            .Setup(s => s.UpdateCategoryAsync(It.IsAny<UpdateCategoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _f.Sut.UpdateCategory(77, dto, CancellationToken.None).ConfigureAwait(true);

        dto.Id.Should().Be(77);
    }

    [Fact]
    public async Task UpdateCategory_WhenValidationFails_ShouldThrowValidationException()
    {
        var dto = new UpdateCategoryDto { Name = "" };

        _f.UpdateCategoryValidator
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminControllerFixture.InvalidResult("Name", "Required"));

        var act = () => _f.Sut.UpdateCategory(1, dto, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().ConfigureAwait(true);
    }

    #endregion

    #region DeleteCategory

    [Fact]
    public async Task DeleteCategory_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.DeleteCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.DeleteCategory(5, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCategory_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.DeleteCategoryAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.DeleteCategory(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ToggleCategoryStatus

    [Fact]
    public async Task ToggleCategoryStatus_WhenSuccessful_ShouldReturnNoContent()
    {
        _f.AdminService
            .Setup(s => s.ToggleCategoryStatusAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _f.Sut.ToggleCategoryStatus(5, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ToggleCategoryStatus_WhenNotFound_ShouldReturnNotFound()
    {
        _f.AdminService
            .Setup(s => s.ToggleCategoryStatusAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _f.Sut.ToggleCategoryStatus(99, CancellationToken.None).ConfigureAwait(true);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}
