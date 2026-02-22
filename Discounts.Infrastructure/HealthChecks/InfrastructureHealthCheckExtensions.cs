using Discounts.Persistance.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Discounts.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods that attach infrastructure-level health checks
/// (SQL Server connectivity and EF Core <see cref="ApplicationDbContext"/> migrations)
/// to an <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class InfrastructureHealthCheckExtensions
{
    /// <summary>
    /// Adds a SQL Server reachability check and an EF Core
    /// <see cref="ApplicationDbContext"/> pending-migration check.
    /// </summary>
    /// <param name="builder">The health-checks builder to extend.</param>
    /// <param name="connectionString">SQL Server connection string.</param>
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder builder,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder
            .AddSqlServer(
                connectionString,
                name: "sql-server",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql", "infrastructure"])
            .AddDbContextCheck<ApplicationDbContext>(
                name: "ef-core",
                failureStatus: HealthStatus.Degraded,
                tags: ["db", "ef", "infrastructure"]);

        return builder;
    }
}
