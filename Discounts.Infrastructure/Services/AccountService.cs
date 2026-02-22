using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;

namespace Discounts.Infrastructure.Services;

/// <summary>
/// Cookie-based authentication service that wraps ASP.NET Identity's
/// UserManager and SignInManager behind a clean application boundary.
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AccountResultModel> RegisterAsync(RegisterAccountModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

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

        if (!result.Succeeded)
        {
            return new AccountResultModel
            {
                Succeeded = false,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        var roleToAssign = model.Role switch
        {
            Roles.Merchant => Roles.Merchant,
            Roles.Customer => Roles.Customer,
            _ => Roles.Customer
        };

        await _userManager.AddToRoleAsync(user, roleToAssign).ConfigureAwait(false);
        await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

        return new AccountResultModel
        {
            Succeeded = true,
            PrimaryRole = roleToAssign
        };
    }

    public async Task<AccountResultModel> LoginAsync(LoginAccountModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true).ConfigureAwait(false);

        if (result.IsLockedOut)
        {
            return new AccountResultModel { Succeeded = false, IsLockedOut = true };
        }

        if (!result.Succeeded)
        {
            return new AccountResultModel
            {
                Succeeded = false,
                Errors = ["Invalid login attempt."]
            };
        }

        var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        string? primaryRole = null;

        if (user != null)
        {
            if (await _userManager.IsInRoleAsync(user, Roles.Administrator).ConfigureAwait(false))
                primaryRole = Roles.Administrator;
            else if (await _userManager.IsInRoleAsync(user, Roles.Merchant).ConfigureAwait(false))
                primaryRole = Roles.Merchant;
            else if (await _userManager.IsInRoleAsync(user, Roles.Customer).ConfigureAwait(false))
                primaryRole = Roles.Customer;
        }

        return new AccountResultModel
        {
            Succeeded = true,
            PrimaryRole = primaryRole
        };
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync().ConfigureAwait(false);
    }
}
