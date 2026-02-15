using System.Globalization;
using Discounts.Application.Interfaces;
using Discounts.Infrastructure.Data;
using Discounts.Infrastructure.Identity;
using Microsoft.AspNetCore.Localization;

namespace Discounts.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds roles, categories, and system settings on startup.
    /// </summary>
    public static async Task SeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        await RoleSeeder.SeedRolesAsync(services).ConfigureAwait(false);
        await CategorySeeder.SeedCategoriesAsync(services).ConfigureAwait(false);

        var settingsRepo = services.GetRequiredService<ISystemSettingsRepository>();
        await SystemSettingsSeeder.SeedDefaultSettingsAsync(settingsRepo).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the Web middleware pipeline (error handling, localization, auth, routing).
    /// </summary>
    public static WebApplication UseWebPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        var supportedCultures = new[] { new CultureInfo("ka"), new CultureInfo("en") };
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("ka"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        });

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Customer}/{action=Index}/{id?}");

        return app;
    }
}
