using System.Security.Claims;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Web.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Controllers;

[Authorize(Roles = Roles.Customer)]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ICategoryService _categoryService;

    public CustomerController(
        ICustomerService customerService,
        ICategoryService categoryService)
    {
        _customerService = customerService;
        _categoryService = categoryService;
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Browse & Search
    [AllowAnonymous]
    public async Task<IActionResult> Index(int? categoryId, string? search, CancellationToken ct)
    {
        IEnumerable<DiscountModel> models;

        if (categoryId.HasValue)
        {
            models = await _customerService.GetDiscountsByCategoryAsync(categoryId.Value, ct).ConfigureAwait(false);
            ViewBag.SelectedCategoryId = categoryId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            models = await _customerService.SearchDiscountsAsync(search, ct).ConfigureAwait(false);
            ViewBag.SearchTerm = search;
        }
        else
        {
            models = await _customerService.GetActiveDiscountsAsync(ct).ConfigureAwait(false);
        }

        var categories = await _categoryService.GetActiveCategoriesAsync(ct).ConfigureAwait(false);
        ViewBag.Categories = categories;

        return View(models.Adapt<IEnumerable<DiscountDto>>());
    }

    [AllowAnonymous]
    public async Task<IActionResult> Browse(int page = 1, int pageSize = 12, int? categoryId = null, string? search = null, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 100) pageSize = 100;

        var pagedModel = await _customerService.GetActiveDiscountsPagedAsync(page, pageSize, ct).ConfigureAwait(false);

        var viewModel = new PagedDiscountsViewModel
        {
            Items = pagedModel.Items.Adapt<IReadOnlyList<DiscountDto>>(),
            Page = pagedModel.Page,
            PageSize = pagedModel.PageSize,
            TotalCount = pagedModel.TotalCount,
            TotalPages = pagedModel.TotalPages,
            HasPreviousPage = pagedModel.HasPreviousPage,
            HasNextPage = pagedModel.HasNextPage,
            CategoryId = categoryId,
            SearchTerm = search
        };

        var categories = await _categoryService.GetActiveCategoriesAsync(ct).ConfigureAwait(false);
        ViewBag.Categories = categories;

        return View(viewModel);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Filter(DiscountFilterDto filter, CancellationToken ct)
    {
        var filterModel = filter.Adapt<DiscountFilterModel>();
        var models = await _customerService.FilterDiscountsAsync(filterModel, ct).ConfigureAwait(false);
        var categories = await _categoryService.GetActiveCategoriesAsync(ct).ConfigureAwait(false);

        ViewBag.Categories = categories;
        ViewBag.Filter = filter;

        return View("Index", models.Adapt<IEnumerable<DiscountDto>>());
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await _customerService.GetDiscountDetailsAsync(id, ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<DiscountDto>());
    }

    // Booking & Purchase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", new { id }) });

        var resultModel = await _customerService.ReserveCouponAsync(id, userId, ct).ConfigureAwait(false);
        var result = resultModel.Adapt<ReservationResultDto>();

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            TempData["ReservationMinutes"] = result.ReservationMinutes;
            TempData["ExpiresAt"] = result.ExpiresAt;
            return RedirectToAction(nameof(Purchase), new { discountId = id });
        }

        TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Purchase(int discountId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetDiscountDetailsAsync(discountId, ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<DiscountDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePurchase(int discountId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var resultModel = await _customerService.PurchaseCouponAsync(discountId, userId, ct).ConfigureAwait(false);
        var result = resultModel.Adapt<PurchaseResultDto>();

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            TempData["CouponCode"] = result.CouponCode;
            return RedirectToAction(nameof(PurchaseSuccess), new { couponId = result.CouponId });
        }

        TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Purchase), new { discountId });
    }

    [HttpGet]
    public async Task<IActionResult> PurchaseSuccess(int couponId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetCouponDetailsAsync(couponId, userId, ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<CouponDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReservation(int couponId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var success = await _customerService.CancelReservationAsync(couponId, userId, ct).ConfigureAwait(false);

        if (success)
        {
            TempData["Success"] = "ჯავშანი გაუქმდა";
        }
        else
        {
            TempData["Error"] = "ჯავშნის გაუქმება ვერ მოხერხდა";
        }

        return RedirectToAction(nameof(MyCoupons));
    }

    // My Coupons
    [HttpGet]
    public async Task<IActionResult> MyCoupons(string? filter, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        IEnumerable<CouponModel> models = filter switch
        {
            "active" => await _customerService.GetMyActiveCouponsAsync(userId, ct).ConfigureAwait(false),
            "used" => await _customerService.GetMyUsedCouponsAsync(userId, ct).ConfigureAwait(false),
            "expired" => await _customerService.GetMyExpiredCouponsAsync(userId, ct).ConfigureAwait(false),
            _ => await _customerService.GetAllMyCouponsAsync(userId, ct).ConfigureAwait(false)
        };

        ViewBag.Filter = filter;
        return View(models.Adapt<IEnumerable<CouponDto>>());
    }

    [HttpGet]
    public async Task<IActionResult> CouponDetails(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetCouponDetailsAsync(id, userId, ct).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<CouponDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsUsed(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var success = await _customerService.MarkCouponAsUsedAsync(id, userId, ct).ConfigureAwait(false);

        if (success)
        {
            TempData["Success"] = "კუპონი მონიშნულია როგორც გამოყენებული";
        }
        else
        {
            TempData["Error"] = "ოპერაცია ვერ შესრულდა";
        }

        return RedirectToAction(nameof(CouponDetails), new { id });
    }
}
