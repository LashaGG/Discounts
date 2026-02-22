using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Register(string role = Roles.Customer)
    {
        ViewBag.Role = role;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var accountModel = model.Adapt<RegisterAccountModel>();
            var result = await _accountService.RegisterAsync(accountModel).ConfigureAwait(false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", result.PrimaryRole);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var loginModel = model.Adapt<LoginAccountModel>();
            var result = await _accountService.LoginAsync(loginModel).ConfigureAwait(false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return result.PrimaryRole switch
                {
                    Roles.Administrator => RedirectToAction("Index", "Admin"),
                    Roles.Merchant => RedirectToAction("Index", "Merchant"),
                    Roles.Customer => RedirectToAction("Index", "Customer"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account is locked out.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync().ConfigureAwait(false);
        return RedirectToAction("Index", "Customer");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
