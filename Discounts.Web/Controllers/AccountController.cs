using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CompanyName = model.CompanyName,
                CompanyDescription = model.CompanyDescription
            };

            var result = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);

            if (result.Succeeded)
            {
                var roleToAssign = model.Role switch
                {
                    Roles.Merchant => Roles.Merchant,
                    Roles.Customer => Roles.Customer,
                    _ => Roles.Customer
                };

                await _userManager.AddToRoleAsync(user, roleToAssign).ConfigureAwait(false);
                await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

                return RedirectToAction("Index", roleToAssign);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
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
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true).ConfigureAwait(false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect based on user role
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, Roles.Administrator).ConfigureAwait(false))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    if (await _userManager.IsInRoleAsync(user, Roles.Merchant).ConfigureAwait(false))
                    {
                        return RedirectToAction("Index", "Merchant");
                    }
                    if (await _userManager.IsInRoleAsync(user, Roles.Customer).ConfigureAwait(false))
                    {
                        return RedirectToAction("Index", "Customer");
                    }
                }

                return RedirectToAction("Index", "Home");
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
        await _signInManager.SignOutAsync().ConfigureAwait(false);
        return RedirectToAction("Index", "Customer");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
