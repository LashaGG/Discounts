using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;

namespace Discounts.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
            }
        }

        await CreateDefaultAdminAsync(userManager).ConfigureAwait(false);
    }

    private static async Task CreateDefaultAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "Aa123123@";

        var adminUser = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword).ConfigureAwait(false);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Administrator).ConfigureAwait(false);
            }
        }
    }
}
