using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Core;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(
        ICustomerService customerService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    // Browse & Search
    [AllowAnonymous]
    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        IEnumerable<DiscountModel> models;

        if (categoryId.HasValue)
        {
            models = await _customerService.GetDiscountsByCategoryAsync(categoryId.Value).ConfigureAwait(false);
            ViewBag.SelectedCategoryId = categoryId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            models = await _customerService.SearchDiscountsAsync(search).ConfigureAwait(false);
            ViewBag.SearchTerm = search;
        }
        else
        {
            models = await _customerService.GetActiveDiscountsAsync().ConfigureAwait(false);
        }

        var categories = await _categoryService.GetActiveCategoriesAsync().ConfigureAwait(false);
        ViewBag.Categories = categories;

        return View(models.Adapt<IEnumerable<DiscountDto>>());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Filter(DiscountFilterDto filter)
    {
        var filterModel = filter.Adapt<DiscountFilterModel>();
        var models = await _customerService.FilterDiscountsAsync(filterModel).ConfigureAwait(false);
        var categories = await _categoryService.GetActiveCategoriesAsync().ConfigureAwait(false);

        ViewBag.Categories = categories;
        ViewBag.Filter = filter;

        return View("Index", models.Adapt<IEnumerable<DiscountDto>>());
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _customerService.GetDiscountDetailsAsync(id).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<DiscountDto>());
    }

    // Booking & Purchase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", new { id }) });

        var resultModel = await _customerService.ReserveCouponAsync(id, userId).ConfigureAwait(false);
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
    public async Task<IActionResult> Purchase(int discountId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetDiscountDetailsAsync(discountId).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<DiscountDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePurchase(int discountId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var resultModel = await _customerService.PurchaseCouponAsync(discountId, userId).ConfigureAwait(false);
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
    public async Task<IActionResult> PurchaseSuccess(int couponId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetCouponDetailsAsync(couponId, userId).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<CouponDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReservation(int couponId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var success = await _customerService.CancelReservationAsync(couponId, userId).ConfigureAwait(false);

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
    public async Task<IActionResult> MyCoupons(string? filter)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        IEnumerable<CouponModel> models = filter switch
        {
            "active" => await _customerService.GetMyActiveCouponsAsync(userId).ConfigureAwait(false),
            "used" => await _customerService.GetMyUsedCouponsAsync(userId).ConfigureAwait(false),
            "expired" => await _customerService.GetMyExpiredCouponsAsync(userId).ConfigureAwait(false),
            _ => await _customerService.GetAllMyCouponsAsync(userId).ConfigureAwait(false)
        };

        ViewBag.Filter = filter;
        return View(models.Adapt<IEnumerable<CouponDto>>());
    }

    [HttpGet]
    public async Task<IActionResult> CouponDetails(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _customerService.GetCouponDetailsAsync(id, userId).ConfigureAwait(false);
        if (model == null)
            return NotFound();

        return View(model.Adapt<CouponDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsUsed(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var success = await _customerService.MarkCouponAsUsedAsync(id, userId).ConfigureAwait(false);

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
