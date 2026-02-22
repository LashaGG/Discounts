using Discounts.Application.Interfaces;
using Discounts.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Discounts.Infrastructure.Data;

/// <summary>
/// Shared data seeding entry point used by both API and Web hosts.
/// Seeds roles, categories, and default system settings on startup.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAllAsync(IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        await RoleSeeder.SeedRolesAsync(services).ConfigureAwait(false);
        await CategorySeeder.SeedCategoriesAsync(services).ConfigureAwait(false);

        var settingsRepo = services.GetRequiredService<ISystemSettingsRepository>();
        await SystemSettingsSeeder.SeedDefaultSettingsAsync(settingsRepo).ConfigureAwait(false);
    }
}
