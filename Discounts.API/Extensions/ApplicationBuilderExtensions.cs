using Discounts.API.Middleware;
using Discounts.Infrastructure.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

namespace Discounts.API.Extensions;

public static class ApplicationBuilderExtensions
{

    public static async Task SeedDataAsync(this WebApplication app)
    {
        await DataSeeder.SeedAllAsync(app).ConfigureAwait(false);
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
        app.UseCustomHealthChecks();

        return app;
    }

    /// <summary>
    /// Registers two health-check endpoints:
    /// <list type="bullet">
    ///   <item><c>/health/live</c> — liveness probe: no checks, just verifies the process is up.</item>
    ///   <item><c>/health/ready</c> — readiness probe: runs every registered check and returns a
    ///     rich JSON report via <see cref="UIResponseWriter"/>.</item>
    /// </list>
    /// </summary>
    public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
    {
        // Liveness: no health checks are executed — returns 200 as long as the process is alive.
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness: all registered checks run; response formatted for Health Checks UI.
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
