using Discounts.Application.HealthChecks;
using Discounts.Infrastructure.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Discounts.API.Extensions;

/// <summary>
/// Extension methods that wire up all application health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers the <see cref="WorkerHealthRegistry"/> singleton,
    /// the <see cref="WorkerServiceHealthCheck"/>, and all infrastructure
    /// health checks (SQL Server + EF Core).
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="dbConnectionString">SQL Server connection string forwarded to infrastructure checks.</param>
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        string dbConnectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbConnectionString);

        // The registry may already be registered by AddInfrastructure; TryAddSingleton is a no-op in that case.
        services.TryAddSingleton<WorkerHealthRegistry>();

        services
            .AddHealthChecks()
            .AddCheck<WorkerServiceHealthCheck>(
                name: "worker-services",
                tags: ["workers", "background"])
            .AddInfrastructureHealthChecks(dbConnectionString);

        return services;
    }
}
