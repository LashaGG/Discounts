using Discounts.Application.Models;

namespace Discounts.Application.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardModel> GetDashboardAsync(CancellationToken ct = default);

    Task<IEnumerable<UserModel>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IEnumerable<UserModel>> GetUsersByRoleAsync(string role, CancellationToken ct = default);
    Task<UserModel?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<bool> DeactivateUserAsync(string userId, CancellationToken ct = default);
    Task<bool> ActivateUserAsync(string userId, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);
    Task<bool> UpdateUserRoleAsync(string userId, string newRole, CancellationToken ct = default);

    Task<IEnumerable<DiscountModel>> GetPendingDiscountsAsync(CancellationToken ct = default);
    Task<bool> ApproveDiscountAsync(int discountId, string adminId, CancellationToken ct = default);
    Task<bool> RejectDiscountAsync(int discountId, string adminId, string reason, CancellationToken ct = default);
    Task<bool> SuspendDiscountAsync(int discountId, string adminId, CancellationToken ct = default);
    Task<bool> ActivateDiscountAsync(int discountId, string adminId, CancellationToken ct = default);

    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken ct = default);
    Task<bool> UpdateSettingAsync(string key, string value, string adminId, CancellationToken ct = default);
    Task<bool> UpdateMultipleSettingsAsync(Dictionary<string, string> settings, string adminId, CancellationToken ct = default);

    Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<CategoryModel> CreateCategoryAsync(CreateCategoryModel model, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(UpdateCategoryModel model, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<bool> ToggleCategoryStatusAsync(int categoryId, CancellationToken ct = default);
}
