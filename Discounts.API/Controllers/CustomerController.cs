using System.Security.Claims;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet("discounts")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetActiveDiscounts(CancellationToken ct)
    {
        var models = await _customerService.GetActiveDiscountsAsync(ct).ConfigureAwait(false);
        return models.Adapt<List<DiscountDto>>();
    }

    [HttpGet("discounts/category/{categoryId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetByCategory(int categoryId, CancellationToken ct)
    {
        var models = await _customerService.GetDiscountsByCategoryAsync(categoryId, ct).ConfigureAwait(false);
        return models.Adapt<List<DiscountDto>>();
    }

    [HttpGet("discounts/search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> Search([FromQuery] string q, CancellationToken ct)
    {
        var models = await _customerService.SearchDiscountsAsync(q, ct).ConfigureAwait(false);
        return models.Adapt<List<DiscountDto>>();
    }
  
    [HttpGet("discounts/filter")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> Filter([FromQuery] DiscountFilterDto filter, CancellationToken ct)
    {
        var filterModel = filter.Adapt<DiscountFilterModel>();
        var models = await _customerService.FilterDiscountsAsync(filterModel, ct).ConfigureAwait(false);
        return models.Adapt<List<DiscountDto>>();
    }

    [HttpGet("discounts/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountDto>> GetDiscountDetails(int id, CancellationToken ct)
    {
        var model = await _customerService.GetDiscountDetailsAsync(id, ct).ConfigureAwait(false);
        if (model == null) return NotFound();

        return model.Adapt<DiscountDto>();
    }

    [HttpPost("discounts/{id:int}/reserve")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReservationResultDto>> ReserveCoupon(int id, CancellationToken ct)
    {
        var model = await _customerService.ReserveCouponAsync(id, GetUserId(), ct).ConfigureAwait(false);
        var result = model.Adapt<ReservationResultDto>();

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Reservation Failed",
                Detail = result.Message
            });
        }
        return result;
    }

    [HttpPost("discounts/{id:int}/purchase")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchaseResultDto>> PurchaseCoupon(int id, CancellationToken ct)
    {
        var model = await _customerService.PurchaseCouponAsync(id, GetUserId(), ct).ConfigureAwait(false);
        var result = model.Adapt<PurchaseResultDto>();

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Purchase Failed",
                Detail = result.Message
            });
        }

        return result;
    }

    [HttpDelete("coupons/{couponId:int}/reservation")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelReservation(int couponId, CancellationToken ct)
    {
        var result = await _customerService.CancelReservationAsync(couponId, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("coupons")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<IEnumerable<CouponDto>>> GetMyCoupons([FromQuery] string? status, CancellationToken ct)
    {
        var models = status?.ToLower() switch
        {
            "active" => await _customerService.GetMyActiveCouponsAsync(GetUserId(), ct).ConfigureAwait(false),
            "used" => await _customerService.GetMyUsedCouponsAsync(GetUserId(), ct).ConfigureAwait(false),
            "expired" => await _customerService.GetMyExpiredCouponsAsync(GetUserId(), ct).ConfigureAwait(false),
            _ => await _customerService.GetAllMyCouponsAsync(GetUserId(), ct).ConfigureAwait(false)
        };

        return models.Adapt<List<CouponDto>>();
    }
 
    [HttpGet("coupons/{couponId:int}")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouponDto>> GetCouponDetails(int couponId, CancellationToken ct)
    {
        var model = await _customerService.GetCouponDetailsAsync(couponId, GetUserId(), ct).ConfigureAwait(false);
        if (model == null) return NotFound();

        return model.Adapt<CouponDto>();
    }

    [HttpPut("coupons/{couponId:int}/use")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkCouponAsUsed(int couponId, CancellationToken ct)
    {
        var result = await _customerService.MarkCouponAsUsedAsync(couponId, GetUserId(), ct).ConfigureAwait(false);
        return result ? NoContent() : NotFound();
    }
}
