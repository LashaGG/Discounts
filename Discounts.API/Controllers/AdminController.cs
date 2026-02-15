// Copyright (C) TBC Bank. All Rights Reserved.

using System.Security.Claims;
using Discounts.API.Requests;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrator)]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IValidator<CreateCategoryDto> _createCategoryValidator;
    private readonly IValidator<UpdateCategoryDto> _updateCategoryValidator;

    public AdminController(
        IAdminService adminService,
        IValidator<CreateCategoryDto> createCategoryValidator,
        IValidator<UpdateCategoryDto> updateCategoryValidator)
    {
        _adminService = adminService;
        _createCategoryValidator = createCategoryValidator;
        _updateCategoryValidator = updateCategoryValidator;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var model = await _adminService.GetDashboardAsync(ct).ConfigureAwait(false);
        return model.Adapt<AdminDashboardDto>();
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] string? role, CancellationToken ct)
    {
        IEnumerable<UserModel> models;

        if (!string.IsNullOrEmpty(role))
            models = await _adminService.GetUsersByRoleAsync(role, ct).ConfigureAwait(false);
        else
            models = await _adminService.GetAllUsersAsync(ct).ConfigureAwait(false);

        return models.Adapt<List<UserDto>>();
    }

    [HttpGet("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(string userId, CancellationToken ct)
    {
        var model = await _adminService.GetUserByIdAsync(userId, ct).ConfigureAwait(false);
        if (model == null) return NotFound();

        return model.Adapt<UserDto>();
    }

 
    [HttpPut("users/{userId}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(string userId, CancellationToken ct)
    {
        var result = await _adminService.DeactivateUserAsync(userId, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("users/{userId}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(string userId, CancellationToken ct)
    {
        var result = await _adminService.ActivateUserAsync(userId, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken ct)
    {
        var result = await _adminService.DeleteUserAsync(userId, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("users/{userId}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        string userId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
    {
        var result = await _adminService.UpdateUserRoleAsync(userId, request.Role, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("discounts/pending")]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetPendingDiscounts(CancellationToken ct)
    {
        var models = await _adminService.GetPendingDiscountsAsync(ct).ConfigureAwait(false);
        return models.Adapt<List<DiscountDto>>();
    }

    [HttpPut("discounts/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveDiscount(int id, CancellationToken ct)
    {
        var result = await _adminService.ApproveDiscountAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("discounts/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectDiscount(
        int id,
        [FromBody] RejectDiscountRequest request,
        CancellationToken ct)
    {
        var result = await _adminService.RejectDiscountAsync(id, GetUserId(), request.Reason, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("discounts/{id:int}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendDiscount(int id, CancellationToken ct)
    {
        var result = await _adminService.SuspendDiscountAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("discounts/{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateDiscount(int id, CancellationToken ct)
    {
        var result = await _adminService.ActivateDiscountAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }
    [HttpGet("settings")]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings(CancellationToken ct)
    {
        var settings = await _adminService.GetAllSettingsAsync(ct).ConfigureAwait(false);
        return settings;
    }

    [HttpPut("settings/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSetting(
        string key,
        [FromBody] UpdateSettingRequest request,
        CancellationToken ct)
    {
        await _adminService.UpdateSettingAsync(key, request.Value, GetUserId(), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMultipleSettings(
        [FromBody] Dictionary<string, string> settings,
        CancellationToken ct)
    {
        await _adminService.UpdateMultipleSettingsAsync(settings, GetUserId(), ct).ConfigureAwait(false);
        return NoContent();
    }

 
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(CancellationToken ct)
    {
        var models = await _adminService.GetAllCategoriesAsync(ct).ConfigureAwait(false);
        return models.Adapt<List<CategoryDto>>();
    }
    [HttpPost("categories")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        [FromBody] CreateCategoryDto dto,
        CancellationToken ct)
    {
        var validation = await _createCategoryValidator.ValidateAsync(dto, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var createModel = dto.Adapt<CreateCategoryModel>();
        var created = await _adminService.CreateCategoryAsync(createModel, ct).ConfigureAwait(false);
        var result = created.Adapt<CategoryDto>();

        return CreatedAtAction(nameof(GetCategories), new { id = result.Id }, result);
    }

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken ct)
    {
        dto.Id = id;
        var validation = await _updateCategoryValidator.ValidateAsync(dto, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var updateModel = dto.Adapt<UpdateCategoryModel>();
        var result = await _adminService.UpdateCategoryAsync(updateModel, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var result = await _adminService.DeleteCategoryAsync(id, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("categories/{id:int}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleCategoryStatus(int id, CancellationToken ct)
    {
        var result = await _adminService.ToggleCategoryStatusAsync(id, ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }
}
