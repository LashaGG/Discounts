using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Configuration;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public SystemSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<SystemSettings?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct);
    }

    public async Task<IEnumerable<SystemSettings>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.SystemSettings
            .OrderBy(s => s.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<SystemSettings> CreateOrUpdateAsync(SystemSettings settings, CancellationToken ct = default)
    {
        var existing = await GetByKeyAsync(settings.Key, ct).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Value = settings.Value;
            existing.Description = settings.Description;
            existing.LastModifiedAt = DateTime.UtcNow;
            existing.LastModifiedBy = settings.LastModifiedBy;
            _context.SystemSettings.Update(existing);
        }
        else
        {
            settings.LastModifiedAt = DateTime.UtcNow;
            await _context.SystemSettings.AddAsync(settings, ct).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return existing ?? settings;
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default)
    {
        var setting = await GetByKeyAsync(key, ct).ConfigureAwait(false);
        return setting?.Value ?? defaultValue;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue = 0, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, "", ct).ConfigureAwait(false);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, "", ct).ConfigureAwait(false);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
