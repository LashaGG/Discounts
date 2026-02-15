using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;

namespace Discounts.Web.Extensions;

public static class UserExtensions
{
    public static async Task<bool> IsAdministratorAsync(
        this UserManager<ApplicationUser> userManager, 
        ApplicationUser user)
    {
        return await userManager.IsInRoleAsync(user, Roles.Administrator).ConfigureAwait(false);
    }

    public static async Task<bool> IsMerchantAsync(
        this UserManager<ApplicationUser> userManager, 
        ApplicationUser user)
    {
        return await userManager.IsInRoleAsync(user, Roles.Merchant).ConfigureAwait(false);
    }

    public static async Task<bool> IsCustomerAsync(
        this UserManager<ApplicationUser> userManager, 
        ApplicationUser user)
    {
        return await userManager.IsInRoleAsync(user, Roles.Customer).ConfigureAwait(false);
    }

    public static async Task<string?> GetPrimaryRoleAsync(
        this UserManager<ApplicationUser> userManager, 
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        
        // Priority: Administrator > Merchant > Customer
        if (roles.Contains(Roles.Administrator))
            return Roles.Administrator;
        
        if (roles.Contains(Roles.Merchant))
            return Roles.Merchant;
        
        if (roles.Contains(Roles.Customer))
            return Roles.Customer;

        return null;
    }
}
