using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace Discounts.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IOrderRepository _orderRepository;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        IDiscountRepository discountRepository,
        ICategoryRepository categoryRepository,
        ISystemSettingsRepository settingsRepository,
        IOrderRepository orderRepository)
    {
        _userManager = userManager;
        _discountRepository = discountRepository;
        _categoryRepository = categoryRepository;
        _settingsRepository = settingsRepository;
        _orderRepository = orderRepository;
    }

    // Dashboard
    public async Task<AdminDashboardModel> GetDashboardAsync(CancellationToken ct = default)
    {
        var allDiscounts = (await _discountRepository.GetAllAsync(ct).ConfigureAwait(false)).ToList();
        var allOrders = (await _orderRepository.GetAllAsync(ct).ConfigureAwait(false)).ToList();
        var totalRevenue = await _orderRepository.GetCompletedRevenueSumAsync(ct).ConfigureAwait(false);

        var merchantUsers = await _userManager.GetUsersInRoleAsync("Merchant").ConfigureAwait(false);
        var customerUsers = await _userManager.GetUsersInRoleAsync("Customer").ConfigureAwait(false);

        var recentDiscounts = await _discountRepository.GetRecentWithDetailsAsync(5, ct).ConfigureAwait(false);

        var allUsers = _userManager.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToList();
        var recentUserModels = new List<UserModel>();
        foreach (var user in allUsers)
        {
            var model = user.Adapt<UserModel>();
            model.Roles = (await _userManager.GetRolesAsync(user).ConfigureAwait(false)).ToList();
            recentUserModels.Add(model);
        }

        var totalUserCount = _userManager.Users.Count();

        return new AdminDashboardModel
        {
            TotalUsers = totalUserCount,
            TotalMerchants = merchantUsers.Count,
            TotalCustomers = customerUsers.Count,
            TotalDiscounts = allDiscounts.Count,
            PendingDiscounts = allDiscounts.Count(d => d.Status == DiscountStatus.Pending),
            ActiveDiscounts = allDiscounts.Count(d => d.Status == DiscountStatus.Active),
            TotalOrders = allOrders.Count,
            TotalRevenue = totalRevenue,
            RecentDiscounts = recentDiscounts.Adapt<List<DiscountModel>>(),
            RecentUsers = recentUserModels
        };
    }

    // User Management
    public Task<IEnumerable<UserModel>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
        return MapUsersToModelsAsync(users);
    }

    public async Task<IEnumerable<UserModel>> GetUsersByRoleAsync(string role, CancellationToken ct = default)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(role).ConfigureAwait(false);
        return await MapUsersToModelsAsync(usersInRole.OrderByDescending(u => u.CreatedAt).ToList()).ConfigureAwait(false);
    }

    public async Task<UserModel?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null) return null;

        var model = user.Adapt<UserModel>();
        model.Roles = (await _userManager.GetRolesAsync(user).ConfigureAwait(false)).ToList();
        return model;
    }

    public async Task<bool> DeactivateUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null) return false;

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> ActivateUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null) return false;

        user.IsActive = true;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string newRole, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        await _userManager.RemoveFromRolesAsync(user, currentRoles).ConfigureAwait(false);

        var result = await _userManager.AddToRoleAsync(user, newRole).ConfigureAwait(false);
        return result.Succeeded;
    }

    // Discount Moderation
    public async Task<IEnumerable<DiscountModel>> GetPendingDiscountsAsync(CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetPendingWithDetailsAsync(ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<bool> ApproveDiscountAsync(int discountId, string adminId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);
        if (discount == null || discount.Status != DiscountStatus.Pending)
            return false;

        discount.Status = DiscountStatus.Active;
        discount.ApprovedByAdminId = adminId;
        discount.ApprovedAt = DateTime.UtcNow;
        discount.LastModifiedAt = DateTime.UtcNow;

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RejectDiscountAsync(int discountId, string adminId, string reason, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);
        if (discount == null || discount.Status != DiscountStatus.Pending)
            return false;

        discount.Status = DiscountStatus.Rejected;
        discount.RejectionReason = reason;
        discount.ApprovedByAdminId = adminId;
        discount.LastModifiedAt = DateTime.UtcNow;

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SuspendDiscountAsync(int discountId, string adminId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);
        if (discount == null || discount.Status != DiscountStatus.Active)
            return false;

        discount.Status = DiscountStatus.Suspended;
        discount.LastModifiedAt = DateTime.UtcNow;

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ActivateDiscountAsync(int discountId, string adminId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);
        if (discount == null)
            return false;

        discount.Status = DiscountStatus.Active;
        discount.ApprovedByAdminId = adminId;
        discount.ApprovedAt = DateTime.UtcNow;
        discount.LastModifiedAt = DateTime.UtcNow;

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);
        return true;
    }

    // Settings
    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _settingsRepository.GetAllAsync(ct).ConfigureAwait(false);
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task<bool> UpdateSettingAsync(string key, string value, string adminId, CancellationToken ct = default)
    {
        await _settingsRepository.CreateOrUpdateAsync(new SystemSettings
        {
            Key = key,
            Value = value,
            LastModifiedBy = adminId,
            LastModifiedAt = DateTime.UtcNow
        }, ct).ConfigureAwait(false);

        return true;
    }

    public async Task<bool> UpdateMultipleSettingsAsync(Dictionary<string, string> settings, string adminId, CancellationToken ct = default)
    {
        foreach (var (key, value) in settings)
        {
            await _settingsRepository.CreateOrUpdateAsync(new SystemSettings
            {
                Key = key,
                Value = value,
                LastModifiedBy = adminId,
                LastModifiedAt = DateTime.UtcNow
            }, ct).ConfigureAwait(false);
        }

        return true;
    }

    // Category Management
    public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _categoryRepository.GetAllAsync(ct).ConfigureAwait(false);
        var models = new List<CategoryModel>();

        foreach (var category in categories)
        {
            var model = category.Adapt<CategoryModel>();
            model.DiscountsCount = category.Discounts?.Count ?? 0;
            models.Add(model);
        }

        return models;
    }

    public async Task<CategoryModel> CreateCategoryAsync(CreateCategoryModel model, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var category = model.Adapt<Category>();
        var created = await _categoryRepository.CreateAsync(category, ct).ConfigureAwait(false);
        return created.Adapt<CategoryModel>();
    }

    public async Task<bool> UpdateCategoryAsync(UpdateCategoryModel model, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var category = await _categoryRepository.GetByIdAsync(model.Id, ct).ConfigureAwait(false);
        if (category == null) return false;

        category.Name = model.Name;
        category.Description = model.Description;
        category.IconClass = model.IconClass;
        category.IsActive = model.IsActive;

        await _categoryRepository.UpdateAsync(category, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, ct).ConfigureAwait(false);
        if (category == null) return false;

        if (category.Discounts?.Any() == true)
            throw new InvalidOperationException("კატეგორია ვერ წაიშლება, რადგან მას აქვს ფასდაკლებები მიბმული");

        await _categoryRepository.DeleteAsync(categoryId, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ToggleCategoryStatusAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, ct).ConfigureAwait(false);
        if (category == null) return false;

        category.IsActive = !category.IsActive;
        await _categoryRepository.UpdateAsync(category, ct).ConfigureAwait(false);
        return true;
    }

    private async Task<IEnumerable<UserModel>> MapUsersToModelsAsync(IList<ApplicationUser> users)
    {
        var models = new List<UserModel>();
        foreach (var user in users)
        {
            var model = user.Adapt<UserModel>();
            model.Roles = (await _userManager.GetRolesAsync(user).ConfigureAwait(false)).ToList();
            models.Add(model);
        }
        return models;
    }
}
