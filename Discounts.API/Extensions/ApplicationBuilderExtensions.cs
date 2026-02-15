using Discounts.API.Middleware;
using Discounts.Application.Interfaces;
using Discounts.Infrastructure.Data;
using Discounts.Infrastructure.Identity;
using Serilog;

namespace Discounts.API.Extensions;

public static class ApplicationBuilderExtensions
{

    public static async Task SeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        await RoleSeeder.SeedRolesAsync(services).ConfigureAwait(false);
        await CategorySeeder.SeedCategoriesAsync(services).ConfigureAwait(false);

        var settingsRepo = services.GetRequiredService<ISystemSettingsRepository>();
        await SystemSettingsSeeder.SeedDefaultSettingsAsync(settingsRepo).ConfigureAwait(false);
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Discounts API v1");
                options.DocumentTitle = "Discounts API";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
