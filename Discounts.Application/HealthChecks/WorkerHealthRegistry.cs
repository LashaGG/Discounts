using System.Collections.Concurrent;

namespace Discounts.Application.HealthChecks;

/// <summary>
/// Singleton registry that background workers update with periodic heartbeats.
/// <see cref="WorkerServiceHealthCheck"/> reads this to determine worker liveness.
/// </summary>
public sealed class WorkerHealthRegistry
{
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat = new();

    /// <summary>Records a healthy heartbeat for the given worker.</summary>
    public void ReportHealthy(string workerName)
        => _lastHeartbeat[workerName] = DateTime.UtcNow;

    /// <summary>
    /// Returns <c>true</c> when the worker has reported within <paramref name="maxAge"/>.
    /// Returns <c>false</c> when the worker has never reported or its last report is stale.
    /// </summary>
    public bool IsHealthy(string workerName, TimeSpan maxAge)
        => _lastHeartbeat.TryGetValue(workerName, out var last)
           && DateTime.UtcNow - last <= maxAge;

    /// <summary>Returns a snapshot of all registered heartbeat timestamps.</summary>
    public IReadOnlyDictionary<string, DateTime> GetHeartbeats()
        => _lastHeartbeat;
}
