using System.Security.Claims;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Discounts.Web.Controllers;

[Authorize(Roles = Roles.Administrator)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AdminController(
        IAdminService adminService,
        IStringLocalizer<SharedResource> localizer)
    {
        _adminService = adminService;
        _localizer = localizer;
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Dashboard
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await _adminService.GetDashboardAsync(ct).ConfigureAwait(false);
        return View(model.Adapt<AdminDashboardDto>());
    }

    // User Management
    [HttpGet]
    public async Task<IActionResult> Users(string? role, CancellationToken ct)
    {
        IEnumerable<UserModel> models;

        if (!string.IsNullOrEmpty(role))
        {
            models = await _adminService.GetUsersByRoleAsync(role, ct).ConfigureAwait(false);
        }
        else
        {
            models = await _adminService.GetAllUsersAsync(ct).ConfigureAwait(false);
        }

        ViewBag.SelectedRole = role;
        return View(models.Adapt<IEnumerable<UserDto>>());
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(string id, CancellationToken ct)
    {
        var model = await _adminService.GetUserByIdAsync(id, ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<UserDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(string id, CancellationToken ct)
    {
        var success = await _adminService.DeactivateUserAsync(id, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_UserDeactivated"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_UserDeactivateFailed"].Value;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(string id, CancellationToken ct)
    {
        var success = await _adminService.ActivateUserAsync(id, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_UserActivated"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_UserActivateFailed"].Value;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken ct)
    {
        var currentUserId = GetUserId();
        if (id == currentUserId)
        {
            TempData["Error"] = _localizer["Admin_CannotDeleteSelf"].Value;
            return RedirectToAction(nameof(Users));
        }

        var success = await _adminService.DeleteUserAsync(id, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_UserDeleted"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_UserDeleteFailed"].Value;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(string userId, string newRole, CancellationToken ct)
    {
        var success = await _adminService.UpdateUserRoleAsync(userId, newRole, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_RoleChanged"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_RoleChangeFailed"].Value;
        }

        return RedirectToAction(nameof(UserDetails), new { id = userId });
    }

    // Discount Moderation
    [HttpGet]
    public async Task<IActionResult> Moderation(CancellationToken ct)
    {
        var models = await _adminService.GetPendingDiscountsAsync(ct).ConfigureAwait(false);
        return View(models.Adapt<IEnumerable<DiscountDto>>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDiscount(int id, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return RedirectToAction("Login", "Account");

        var success = await _adminService.ApproveDiscountAsync(id, adminId, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_DiscountApproved"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_DiscountApproveFailed"].Value;
        }

        return RedirectToAction(nameof(Moderation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDiscount(int id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = _localizer["Admin_RejectReasonRequired"].Value;
            return RedirectToAction(nameof(Moderation));
        }

        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return RedirectToAction("Login", "Account");

        var success = await _adminService.RejectDiscountAsync(id, adminId, reason, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_DiscountRejected"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_DiscountRejectFailed"].Value;
        }

        return RedirectToAction(nameof(Moderation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendDiscount(int id, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return RedirectToAction("Login", "Account");

        var success = await _adminService.SuspendDiscountAsync(id, adminId, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_DiscountSuspended"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_DiscountSuspendFailed"].Value;
        }

        return RedirectToAction(nameof(Moderation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateDiscount(int id, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return RedirectToAction("Login", "Account");

        var success = await _adminService.ActivateDiscountAsync(id, adminId, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_DiscountActivated"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_DiscountActivateFailed"].Value;
        }

        return RedirectToAction(nameof(Moderation));
    }

    // Settings Management
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var settings = await _adminService.GetAllSettingsAsync(ct).ConfigureAwait(false);
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(Dictionary<string, string> settings, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return RedirectToAction("Login", "Account");

        var success = await _adminService.UpdateMultipleSettingsAsync(settings, adminId, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_SettingsSaved"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_SettingsSaveFailed"].Value;
        }

        return RedirectToAction(nameof(Settings));
    }

    // Category Management
    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var models = await _adminService.GetAllCategoriesAsync(ct).ConfigureAwait(false);
        return View(models.Adapt<IEnumerable<CategoryDto>>());
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateCategoryDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _adminService.CreateCategoryAsync(model.Adapt<CreateCategoryModel>(), ct).ConfigureAwait(false);
            TempData["Success"] = _localizer["Admin_CategoryCreated"].Value;
            return RedirectToAction(nameof(Categories));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"შეცდომა: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, CancellationToken ct)
    {
        var categories = await _adminService.GetAllCategoriesAsync(ct).ConfigureAwait(false);
        var category = categories.FirstOrDefault(c => c.Id == id);

        if (category == null)
            return NotFound();

        var dto = category.Adapt<UpdateCategoryDto>();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(UpdateCategoryDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var success = await _adminService.UpdateCategoryAsync(model.Adapt<UpdateCategoryModel>(), ct).ConfigureAwait(false);
            if (success)
            {
                TempData["Success"] = _localizer["Admin_CategoryUpdated"].Value;
                return RedirectToAction(nameof(Categories));
            }
            else
            {
                ModelState.AddModelError("", _localizer["Admin_CategoryUpdateFailed"].Value);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"შეცდომა: {ex.Message}");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        try
        {
            await _adminService.DeleteCategoryAsync(id, ct).ConfigureAwait(false);
            TempData["Success"] = _localizer["Admin_CategoryDeleted"].Value;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = _localizer["Admin_CategoryDeleteFailed"].Value;
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryStatus(int id, CancellationToken ct)
    {
        var success = await _adminService.ToggleCategoryStatusAsync(id, ct).ConfigureAwait(false);
        if (success)
        {
            TempData["Success"] = _localizer["Admin_CategoryStatusChanged"].Value;
        }
        else
        {
            TempData["Error"] = _localizer["Admin_CategoryStatusChangeFailed"].Value;
        }

        return RedirectToAction(nameof(Categories));
    }
}
