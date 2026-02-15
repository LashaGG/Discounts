using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Configuration;

namespace Discounts.Infrastructure.Data;

public static class SystemSettingsSeeder
{
    public static async Task SeedDefaultSettingsAsync(ISystemSettingsRepository repository)
    {
        var defaultSettings = new List<SystemSettings>
        {
            new()
            {
                Key = SettingsKeys.ReservationDuration,
                Value = "30",
                Description = "Reservation duration in minutes"
            },
            new()
            {
                Key = SettingsKeys.MerchantEditWindow,
                Value = "24",
                Description = "Time window (hours) for merchant to edit discount after creation"
            },
            new()
            {
                Key = SettingsKeys.MaxDiscountsPerMerchant,
                Value = "100",
                Description = "Maximum number of active discounts per merchant"
            },
            new()
            {
                Key = SettingsKeys.MaxCouponsPerDiscount,
                Value = "10000",
                Description = "Maximum number of coupons per discount"
            },
            new()
            {
                Key = SettingsKeys.AutoApprovalEnabled,
                Value = "false",
                Description = "Auto-approve discounts without admin review"
            },
            new()
            {
                Key = SettingsKeys.SiteName,
                Value = "Discounts.ge",
                Description = "Site name displayed in UI"
            },
            new()
            {
                Key = SettingsKeys.SupportEmail,
                Value = "support@discounts.ge",
                Description = "Support email for customer inquiries"
            }
        };

        foreach (var setting in defaultSettings)
        {
            var existing = await repository.GetByKeyAsync(setting.Key).ConfigureAwait(false);
            if (existing == null)
            {
                await repository.CreateOrUpdateAsync(setting).ConfigureAwait(false);
            }
        }
    }
}
