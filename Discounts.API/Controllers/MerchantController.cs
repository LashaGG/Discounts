using System.Security.Claims;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Domain.Enums;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Merchant)]
[Produces("application/json")]
public class MerchantController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IValidator<CreateDiscountDto> _createValidator;
    private readonly IValidator<UpdateDiscountDto> _updateValidator;

    public MerchantController(
        IMerchantService merchantService,
        IValidator<CreateDiscountDto> createValidator,
        IValidator<UpdateDiscountDto> updateValidator)
    {
        _merchantService = merchantService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token");
 
    [HttpGet("dashboard")]
    public async Task<ActionResult<MerchantDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var model = await _merchantService.GetDashboardAsync(GetUserId(), ct).ConfigureAwait(false);
        return model.Adapt<MerchantDashboardDto>();
    }

    [HttpGet("discounts")]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetMyDiscounts(
        [FromQuery] DiscountStatus? status,
        CancellationToken ct)
    {
        IEnumerable<DiscountModel> models;

        if (status.HasValue)
            models = await _merchantService.GetMerchantDiscountsByStatusAsync(GetUserId(), status.Value, ct).ConfigureAwait(false);
        else
            models = await _merchantService.GetMerchantDiscountsAsync(GetUserId(), ct).ConfigureAwait(false);

        return models.Adapt<List<DiscountDto>>();
    }

    [HttpGet("discounts/{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountDto>> GetDiscount(int id, CancellationToken ct)
    {
        var model = await _merchantService.GetDiscountByIdAsync(id, GetUserId(), ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return model.Adapt<DiscountDto>();
    }

    [HttpPost("discounts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscountDto>> CreateDiscount(
        [FromBody] CreateDiscountDto dto,
        CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var createModel = dto.Adapt<CreateDiscountModel>();
        var created = await _merchantService.CreateDiscountAsync(createModel, GetUserId(), ct).ConfigureAwait(false);
        var result = created.Adapt<DiscountDto>();

        return CreatedAtAction(nameof(GetDiscount), new { id = result.Id }, result);
    }

    [HttpPut("discounts/{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscountDto>> UpdateDiscount(
        int id,
        [FromBody] UpdateDiscountDto dto,
        CancellationToken ct)
    {
        dto.Id = id;
        var validation = await _updateValidator.ValidateAsync(dto, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var updateModel = dto.Adapt<UpdateDiscountModel>();
        var updated = await _merchantService.UpdateDiscountAsync(updateModel, GetUserId(), ct).ConfigureAwait(false);
        return updated.Adapt<DiscountDto>();
    }

    [HttpDelete("discounts/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDiscount(int id, CancellationToken ct)
    {
        var result = await _merchantService.DeleteDiscountAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("discounts/{id:int}/sales")]
    public async Task<ActionResult<IEnumerable<SalesHistoryDto>>> GetSalesHistory(int id, CancellationToken ct)
    {
        var models = await _merchantService.GetSalesHistoryAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return models.Adapt<List<SalesHistoryDto>>();
    }

    [HttpGet("discounts/{id:int}/can-edit")]
    public async Task<ActionResult<object>> CanEdit(int id, CancellationToken ct)
    {
        var canEdit = await _merchantService.CanEditDiscountAsync(id, GetUserId(), ct).ConfigureAwait(false);
        return new { canEdit };
    }
}
