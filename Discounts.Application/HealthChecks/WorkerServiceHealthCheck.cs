using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Discounts.Application.HealthChecks;

/// <summary>
/// Health check that reports the liveness of the background worker services
/// (<c>ReservationCleanupService</c> and <c>OfferExpirationService</c>) by
/// reading heartbeats written to <see cref="WorkerHealthRegistry"/>.
/// A worker is considered unhealthy when it has not reported within the last
/// 15 minutes — twice the longest worker cycle (10 min) plus a safety margin.
/// </summary>
public sealed class WorkerServiceHealthCheck : IHealthCheck
{
    // Names must match the values passed to WorkerHealthRegistry.ReportHealthy in each service.
    private static readonly string[] RequiredWorkers =
    [
        "ReservationCleanupService",
        "OfferExpirationService"
    ];

    // Allow two full cycles of the slowest worker (10 min) plus a buffer.
    private static readonly TimeSpan MaxHeartbeatAge = TimeSpan.FromMinutes(15);

    private readonly WorkerHealthRegistry _registry;

    public WorkerServiceHealthCheck(WorkerHealthRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var staleWorkers = RequiredWorkers
            .Where(w => !_registry.IsHealthy(w, MaxHeartbeatAge))
            .ToList();

        if (staleWorkers.Count == 0)
            return Task.FromResult(HealthCheckResult.Healthy("All background worker services are running."));

        var heartbeats = _registry.GetHeartbeats();
        var data = staleWorkers.ToDictionary(
            w => w,
            w => (object)(heartbeats.TryGetValue(w, out var ts)
                ? $"Last heartbeat: {ts:O}"
                : "No heartbeat recorded"));

        return Task.FromResult(HealthCheckResult.Unhealthy(
            $"Worker(s) not responding: {string.Join(", ", staleWorkers)}",
            data: data));
    }
}
