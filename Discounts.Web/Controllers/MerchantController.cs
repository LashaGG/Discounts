using System.Security.Claims;
using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Domain.Enums;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Discounts.Web.Controllers;

[Authorize(Roles = Roles.Merchant)]
public class MerchantController : Controller
{
    private readonly IMerchantService _merchantService;
    private readonly ICategoryService _categoryService;

    public MerchantController(
        IMerchantService merchantService,
        ICategoryService categoryService)
    {
        _merchantService = merchantService;
        _categoryService = categoryService;
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Dashboard
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _merchantService.GetDashboardAsync(userId, ct).ConfigureAwait(false);
        return View(model.Adapt<MerchantDashboardDto>());
    }

    // My Discounts List
    public async Task<IActionResult> MyDiscounts(DiscountStatus? status, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        IEnumerable<DiscountModel> models;

        if (status.HasValue)
        {
            models = await _merchantService.GetMerchantDiscountsByStatusAsync(userId, status.Value, ct).ConfigureAwait(false);
        }
        else
        {
            models = await _merchantService.GetMerchantDiscountsAsync(userId, ct).ConfigureAwait(false);
        }

        ViewBag.CurrentStatus = status;
        return View(models.Adapt<IEnumerable<DiscountDto>>());
    }

    // Create GET
    [HttpGet]
    public async Task<IActionResult> CreateDiscount(CancellationToken ct)
    {
        await PopulateCategoriesAsync(ct: ct).ConfigureAwait(false);
        return View();
    }

    // Create POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDiscount(CreateDiscountDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(ct: ct).ConfigureAwait(false);
            return View(model);
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var created = await _merchantService.CreateDiscountAsync(model.Adapt<CreateDiscountModel>(), userId, ct).ConfigureAwait(false);
            TempData["Success"] = "ფასდაკლება წარმატებით შეიქმნა. ის გაიგზავნება ადმინისტრატორის დასამტკიცებლად.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"შეცდომა: {ex.Message}");
            await PopulateCategoriesAsync(ct: ct).ConfigureAwait(false);
            return View(model);
        }
    }

    // Edit GET
    [HttpGet]
    public async Task<IActionResult> EditDiscount(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId, ct).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        var canEdit = await _merchantService.CanEditDiscountAsync(id, userId, ct).ConfigureAwait(false);
        if (!canEdit)
        {
            TempData["Error"] = "ფასდაკლების რედაქტირება შეუძლებელია. რედაქტირება შესაძლებელია მხოლოდ 24 საათის განმავლობაში შექმნის შემდეგ, ან თუ სტატუსი არის 'მოლოდინში' ან 'უარყოფილი'.";
            return RedirectToAction(nameof(MyDiscounts));
        }

        var dto = discount.Adapt<UpdateDiscountDto>();

        await PopulateCategoriesAsync(discount.CategoryId, ct).ConfigureAwait(false);
        return View(dto);
    }

    // Edit POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDiscount(UpdateDiscountDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId, ct).ConfigureAwait(false);
            return View(model);
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            await _merchantService.UpdateDiscountAsync(model.Adapt<UpdateDiscountModel>(), userId, ct).ConfigureAwait(false);
            TempData["Success"] = "ფასდაკლება წარმატებით განახლდა.";
            return RedirectToAction(nameof(MyDiscounts));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "თქვენ არ გაქვთ ამ ფასდაკლების რედაქტირების უფლება.";
            return RedirectToAction(nameof(MyDiscounts));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MyDiscounts));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"შეცდომა: {ex.Message}");
            await PopulateCategoriesAsync(model.CategoryId, ct).ConfigureAwait(false);
            return View(model);
        }
    }

    // Details GET
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId, ct).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        return View(discount.Adapt<DiscountDto>());
    }

    // Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var success = await _merchantService.DeleteDiscountAsync(id, userId, ct).ConfigureAwait(false);
            if (success)
            {
                TempData["Success"] = "ფასდაკლება წარმატებით წაიშალა.";
            }
            else
            {
                TempData["Error"] = "ფასდაკლების წაშლა ვერ მოხერხდა.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(MyDiscounts));
    }

    // Sales History
    [HttpGet]
    public async Task<IActionResult> SalesHistory(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId, ct).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        var salesHistory = await _merchantService.GetSalesHistoryAsync(id, userId, ct).ConfigureAwait(false);
        ViewBag.DiscountTitle = discount.Title;
        ViewBag.DiscountId = id;

        return View(salesHistory.Adapt<IEnumerable<SalesHistoryDto>>());
    }

    private async Task PopulateCategoriesAsync(int? selectedCategoryId = null, CancellationToken ct = default)
    {
        var categories = await _categoryService.GetActiveCategoriesAsync(ct).ConfigureAwait(false);
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
    }
}
