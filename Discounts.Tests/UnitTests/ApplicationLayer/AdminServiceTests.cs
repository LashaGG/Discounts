using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;
using Discounts.Tests.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

public class AdminServiceTests : IClassFixture<AdminServiceTestsFixture>
{
    private readonly IAdminService _sut;
    private readonly Mock<IDiscountRepository> _discountRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly CancellationToken _testCt;

    public AdminServiceTests(AdminServiceTestsFixture fixture)
    {
        _sut = fixture.Sut;
        _discountRepoMock = fixture.DiscountRepoMock;
        _categoryRepoMock = fixture.CategoryRepoMock;
        _settingsRepoMock = fixture.SettingsRepoMock;
        _orderRepoMock = fixture.OrderRepoMock;
        _userManagerMock = fixture.UserManagerMock;
        _testCt = fixture.TestCt;

        _discountRepoMock.Invocations.Clear();
        _categoryRepoMock.Invocations.Clear();
        _settingsRepoMock.Invocations.Clear();
        _orderRepoMock.Invocations.Clear();
        _userManagerMock.Invocations.Clear();
        fixture.LocalizerMock.Invocations.Clear();
    }

    [Fact]
    public async Task GetPendingDiscountsAsync_ReturnsMappedModels()
    {
        var discounts = new List<Discount> { AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Pending) };
        _discountRepoMock
            .Setup(r => r.GetPendingWithDetailsAsync(_testCt))
            .ReturnsAsync(discounts);

        var result = await _sut.GetPendingDiscountsAsync(_testCt).ConfigureAwait(true);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task ApproveDiscountAsync_WhenPending_ReturnsTrue()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Pending);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);
        _discountRepoMock
            .Setup(r => r.UpdateAsync(discount, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.ApproveDiscountAsync(1, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        discount.Status.Should().Be(DiscountStatus.Active);
        discount.ApprovedByAdminId.Should().Be("admin-1");
        discount.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveDiscountAsync_WhenNotPending_ReturnsFalse()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Active);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.ApproveDiscountAsync(1, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveDiscountAsync_WhenNotFound_ReturnsFalse()
    {
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Discount?)null);

        var result = await _sut.ApproveDiscountAsync(99, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RejectDiscountAsync_WhenPending_ReturnsTrueAndSetsReason()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Pending);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);
        _discountRepoMock
            .Setup(r => r.UpdateAsync(discount, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.RejectDiscountAsync(1, "admin-1", "Low quality", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        discount.Status.Should().Be(DiscountStatus.Rejected);
        discount.RejectionReason.Should().Be("Low quality");
        discount.ApprovedByAdminId.Should().Be("admin-1");
    }

    [Fact]
    public async Task RejectDiscountAsync_WhenNotPending_ReturnsFalse()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Active);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.RejectDiscountAsync(1, "admin-1", "reason", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SuspendDiscountAsync_WhenActive_ReturnsTrue()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Active);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);
        _discountRepoMock
            .Setup(r => r.UpdateAsync(discount, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.SuspendDiscountAsync(1, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        discount.Status.Should().Be(DiscountStatus.Suspended);
    }

    [Fact]
    public async Task SuspendDiscountAsync_WhenNotActive_ReturnsFalse()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Pending);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);

        var result = await _sut.SuspendDiscountAsync(1, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SuspendDiscountAsync_WhenNotFound_ReturnsFalse()
    {
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Discount?)null);

        var result = await _sut.SuspendDiscountAsync(99, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateDiscountAsync_WhenFound_ReturnsTrue()
    {
        var discount = AdminServiceTestsFixture.CreateDiscount(1, status: DiscountStatus.Suspended);
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(discount);
        _discountRepoMock
            .Setup(r => r.UpdateAsync(discount, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.ActivateDiscountAsync(1, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        discount.Status.Should().Be(DiscountStatus.Active);
        discount.ApprovedByAdminId.Should().Be("admin-1");
        discount.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ActivateDiscountAsync_WhenNotFound_ReturnsFalse()
    {
        _discountRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Discount?)null);

        var result = await _sut.ActivateDiscountAsync(99, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllSettingsAsync_ReturnsDictionary()
    {
        var settings = new List<SystemSettings>
        {
            new() { Key = "SiteName", Value = "Discounts" },
            new() { Key = "SupportEmail", Value = "support@test.com" }
        };
        _settingsRepoMock
            .Setup(r => r.GetAllAsync(_testCt))
            .ReturnsAsync(settings);

        var result = await _sut.GetAllSettingsAsync(_testCt).ConfigureAwait(true);

        result.Should().HaveCount(2);
        result["SiteName"].Should().Be("Discounts");
        result["SupportEmail"].Should().Be("support@test.com");
    }

    [Fact]
    public async Task UpdateSettingAsync_CallsCreateOrUpdate_ReturnsTrue()
    {
        _settingsRepoMock
            .Setup(r => r.CreateOrUpdateAsync(It.Is<SystemSettings>(s =>
                s.Key == "SiteName" && s.Value == "NewName" && s.LastModifiedBy == "admin-1"), _testCt))
            .ReturnsAsync(new SystemSettings { Key = "SiteName", Value = "NewName" });

        var result = await _sut.UpdateSettingAsync("SiteName", "NewName", "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        _settingsRepoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<SystemSettings>(), _testCt), Times.Once);
    }

    [Fact]
    public async Task UpdateMultipleSettingsAsync_CallsCreateOrUpdateForEachKey()
    {
        var settings = new Dictionary<string, string>
        {
            ["SiteName"] = "Test",
            ["SupportEmail"] = "a@b.com"
        };

        _settingsRepoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<SystemSettings>(), _testCt))
            .ReturnsAsync(new SystemSettings());

        var result = await _sut.UpdateMultipleSettingsAsync(settings, "admin-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        _settingsRepoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<SystemSettings>(), _testCt), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsMappedModelsWithDiscountsCount()
    {
        var category = AdminServiceTestsFixture.CreateCategory(1, "Food");
        category.Discounts = new List<Discount>
        {
            AdminServiceTestsFixture.CreateDiscount(),
            AdminServiceTestsFixture.CreateDiscount(2)
        };

        _categoryRepoMock
            .Setup(r => r.GetAllAsync(_testCt))
            .ReturnsAsync(new List<Category> { category });

        var result = (await _sut.GetAllCategoriesAsync(_testCt).ConfigureAwait(true)).ToList();

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Food");
        result[0].DiscountsCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenValid_ReturnsCreatedModel()
    {
        var model = new CreateCategoryModel { Name = "Travel", Description = "Trips" };

        _categoryRepoMock
            .Setup(r => r.CreateAsync(It.Is<Category>(c => c.Name == "Travel"), _testCt))
            .ReturnsAsync((Category c, CancellationToken _) =>
            {
                c.Id = 10;
                return c;
            });

        var result = await _sut.CreateCategoryAsync(model, _testCt).ConfigureAwait(true);

        result.Id.Should().Be(10);
        result.Name.Should().Be("Travel");
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenModelIsNull_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _sut.CreateCategoryAsync(null!, _testCt);

        await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenFound_ReturnsTrue()
    {
        var category = AdminServiceTestsFixture.CreateCategory(1, "Food");
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(category);
        _categoryRepoMock
            .Setup(r => r.UpdateAsync(category, _testCt))
            .Returns(Task.CompletedTask);

        var model = new UpdateCategoryModel
        {
            Id = 1,
            Name = "Updated",
            Description = "New desc",
            IconClass = "fa-star",
            IsActive = false
        };

        var result = await _sut.UpdateCategoryAsync(model, _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        category.Name.Should().Be("Updated");
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenNotFound_ReturnsFalse()
    {
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Category?)null);

        var model = new UpdateCategoryModel { Id = 99, Name = "X" };

        var result = await _sut.UpdateCategoryAsync(model, _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenModelIsNull_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _sut.UpdateCategoryAsync(null!, _testCt);

        await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenFoundAndNoDiscounts_ReturnsTrue()
    {
        var category = AdminServiceTestsFixture.CreateCategory(1);
        category.Discounts = new List<Discount>();
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(category);
        _categoryRepoMock
            .Setup(r => r.DeleteAsync(1, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteCategoryAsync(1, _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenNotFound_ReturnsFalse()
    {
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Category?)null);

        var result = await _sut.DeleteCategoryAsync(99, _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenHasDiscounts_ThrowsInvalidOperationException()
    {
        var category = AdminServiceTestsFixture.CreateCategory(1);
        category.Discounts = new List<Discount> { AdminServiceTestsFixture.CreateDiscount() };
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(category);

        Func<Task> act = () => _sut.DeleteCategoryAsync(1, _testCt);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task ToggleCategoryStatusAsync_WhenFound_TogglesAndReturnsTrue()
    {
        var category = AdminServiceTestsFixture.CreateCategory(1, isActive: true);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(1, _testCt))
            .ReturnsAsync(category);
        _categoryRepoMock
            .Setup(r => r.UpdateAsync(category, _testCt))
            .Returns(Task.CompletedTask);

        var result = await _sut.ToggleCategoryStatusAsync(1, _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleCategoryStatusAsync_WhenNotFound_ReturnsFalse()
    {
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(99, _testCt))
            .ReturnsAsync((Category?)null);

        var result = await _sut.ToggleCategoryStatusAsync(99, _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenFound_ReturnsMappedModelWithRoles()
    {
        var user = AdminServiceTestsFixture.CreateMerchant("user-1");
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Merchant" });

        var result = await _sut.GetUserByIdAsync("user-1", _testCt).ConfigureAwait(true);

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-1");
        result.Roles.Should().Contain("Merchant");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenNotFound_ReturnsNull()
    {
        _userManagerMock
            .Setup(u => u.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetUserByIdAsync("nonexistent", _testCt).ConfigureAwait(true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenFound_SetsInactiveAndReturnsTrue()
    {
        var user = AdminServiceTestsFixture.CreateMerchant("user-1");
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.DeactivateUserAsync("user-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenNotFound_ReturnsFalse()
    {
        _userManagerMock
            .Setup(u => u.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.DeactivateUserAsync("nonexistent", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateUserAsync_WhenFound_SetsActiveAndReturnsTrue()
    {
        var user = AdminServiceTestsFixture.CreateMerchant("user-1");
        user.IsActive = false;
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ActivateUserAsync("user-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenFound_ReturnsTrue()
    {
        var user = AdminServiceTestsFixture.CreateMerchant("user-1");
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.DeleteUserAsync("user-1", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotFound_ReturnsFalse()
    {
        _userManagerMock
            .Setup(u => u.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.DeleteUserAsync("nonexistent", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenFound_RemovesOldRolesAndAddsNew()
    {
        var user = AdminServiceTestsFixture.CreateMerchant("user-1");
        _userManagerMock
            .Setup(u => u.FindByIdAsync("user-1"))
            .ReturnsAsync(user);
        _userManagerMock
            .Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Merchant" });
        _userManagerMock
            .Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.AddToRoleAsync(user, "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateUserRoleAsync("user-1", "Customer", _testCt).ConfigureAwait(true);

        result.Should().BeTrue();
        _userManagerMock.Verify(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(user, "Customer"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenNotFound_ReturnsFalse()
    {
        _userManagerMock
            .Setup(u => u.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.UpdateUserRoleAsync("nonexistent", "Customer", _testCt).ConfigureAwait(true);

        result.Should().BeFalse();
    }
}
