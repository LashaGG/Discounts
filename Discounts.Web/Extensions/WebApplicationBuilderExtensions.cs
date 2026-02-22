using System.Globalization;
using Discounts.Infrastructure.Data;
using Discounts.Web.Middleware;
using Microsoft.AspNetCore.Localization;
using Serilog;

namespace Discounts.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds roles, categories, and system settings on startup.
    /// </summary>
    public static async Task SeedDataAsync(this WebApplication app)
    {
        await DataSeeder.SeedAllAsync(app).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the Web middleware pipeline (error handling, localization, auth, routing).
    /// </summary>
    public static WebApplication UseWebPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        var kaCulture = new CultureInfo("ka");
        kaCulture.NumberFormat.CurrencySymbol = "$";

        var enCulture = new CultureInfo("en");
        enCulture.NumberFormat.CurrencySymbol = "$";

        var supportedCultures = new[] { kaCulture, enCulture };
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
