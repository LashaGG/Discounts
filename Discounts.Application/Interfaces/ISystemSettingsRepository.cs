using Discounts.Domain.Entities.Configuration;

namespace Discounts.Application.Interfaces;

public interface ISystemSettingsRepository
{
    Task<SystemSettings?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IEnumerable<SystemSettings>> GetAllAsync(CancellationToken ct = default);
    Task<SystemSettings> CreateOrUpdateAsync(SystemSettings settings, CancellationToken ct = default);
    Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default);
    Task<int> GetIntValueAsync(string key, int defaultValue = 0, CancellationToken ct = default);
    Task<bool> GetBoolValueAsync(string key, bool defaultValue = false, CancellationToken ct = default);
}
