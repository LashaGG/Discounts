using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Discounts.Web.Controllers;

[Authorize(Roles = "Merchant")]
public class MerchantController : Controller
{
    private readonly IMerchantService _merchantService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MerchantController(
        IMerchantService merchantService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _merchantService = merchantService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    // Dashboard
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = await _merchantService.GetDashboardAsync(userId).ConfigureAwait(false);
        return View(model.Adapt<MerchantDashboardDto>());
    }

    // My Discounts List
    public async Task<IActionResult> MyDiscounts(DiscountStatus? status)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        IEnumerable<DiscountModel> models;

        if (status.HasValue)
        {
            models = await _merchantService.GetMerchantDiscountsByStatusAsync(userId, status.Value).ConfigureAwait(false);
        }
        else
        {
            models = await _merchantService.GetMerchantDiscountsAsync(userId).ConfigureAwait(false);
        }

        ViewBag.CurrentStatus = status;
        return View(models.Adapt<IEnumerable<DiscountDto>>());
    }

    // Create GET
    [HttpGet]
    public async Task<IActionResult> CreateDiscount()
    {
        await PopulateCategoriesAsync().ConfigureAwait(false);
        return View();
    }

    // Create POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDiscount(CreateDiscountDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync().ConfigureAwait(false);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            await _merchantService.CreateDiscountAsync(model.Adapt<CreateDiscountModel>(), userId).ConfigureAwait(false);
            TempData["Success"] = "ფასდაკლება წარმატებით შეიქმნა. ის გაიგზავნება ადმინისტრატორის დასამტკიცებლად.";
            return RedirectToAction(nameof(MyDiscounts));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"შეცდომა: {ex.Message}");
            await PopulateCategoriesAsync().ConfigureAwait(false);
            return View(model);
        }
    }

    // Edit GET
    [HttpGet]
    public async Task<IActionResult> EditDiscount(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        var canEdit = await _merchantService.CanEditDiscountAsync(id, userId).ConfigureAwait(false);
        if (!canEdit)
        {
            TempData["Error"] = "ფასდაკლების რედაქტირება შეუძლებელია. რედაქტირება შესაძლებელია მხოლოდ 24 საათის განმავლობაში შექმნის შემდეგ, ან თუ სტატუსი არის 'მოლოდინში' ან 'უარყოფილი'.";
            return RedirectToAction(nameof(MyDiscounts));
        }

        var dto = discount.Adapt<UpdateDiscountDto>();

        await PopulateCategoriesAsync(discount.CategoryId).ConfigureAwait(false);
        return View(dto);
    }

    // Edit POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDiscount(UpdateDiscountDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId).ConfigureAwait(false);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            await _merchantService.UpdateDiscountAsync(model.Adapt<UpdateDiscountModel>(), userId).ConfigureAwait(false);
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
            await PopulateCategoriesAsync(model.CategoryId).ConfigureAwait(false);
            return View(model);
        }
    }

    // Details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        return View(discount.Adapt<DiscountDto>());
    }

    // Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var success = await _merchantService.DeleteDiscountAsync(id, userId).ConfigureAwait(false);
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
    public async Task<IActionResult> SalesHistory(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var discount = await _merchantService.GetDiscountByIdAsync(id, userId).ConfigureAwait(false);
        if (discount == null)
            return NotFound();

        var salesHistory = await _merchantService.GetSalesHistoryAsync(id, userId).ConfigureAwait(false);
        ViewBag.DiscountTitle = discount.Title;
        ViewBag.DiscountId = id;

        return View(salesHistory.Adapt<IEnumerable<SalesHistoryDto>>());
    }

    private async Task PopulateCategoriesAsync(int? selectedCategoryId = null)
    {
        var categories = await _categoryService.GetActiveCategoriesAsync().ConfigureAwait(false);
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
    }
}
