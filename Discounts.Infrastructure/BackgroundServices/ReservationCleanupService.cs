using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Infrastructure.BackgroundServices;

public class ReservationCleanupService : BackgroundService
{
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReservationCleanupService(
        ILogger<ReservationCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredReservationsAsync().ConfigureAwait(false);
                await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired reservations");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Reservation Cleanup Service stopped");
    }

    private async Task CleanupExpiredReservationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();

        // Get reservation duration from settings
        var reservationMinutes = await settingsRepo.GetIntValueAsync(
            SettingsKeys.ReservationDuration, 30).ConfigureAwait(false);

        var expirationThreshold = DateTime.UtcNow.AddMinutes(-reservationMinutes);

        // Find all expired reservations
        var expiredReservations = await context.Coupons
            .Include(c => c.Discount)
            .Where(c => c.Status == CouponStatus.Reserved &&
                       c.ReservedAt.HasValue &&
                       c.ReservedAt.Value < expirationThreshold)
            .ToListAsync().ConfigureAwait(false);
        if (!expiredReservations.Any())
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("Found {Count} expired reservations", expiredReservations.Count);

        foreach (var coupon in expiredReservations)
        {
            try
            {
                // Release the coupon
                coupon.Status = CouponStatus.Available;
                coupon.CustomerId = null;
                coupon.ReservedAt = null;

                // Increase available coupons count
                coupon.Discount.AvailableCoupons++;

                _logger.LogInformation(
                    "Released expired reservation - Coupon: {CouponId}, Discount: {DiscountId}",
                    coupon.Id, coupon.DiscountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release coupon {CouponId}", coupon.Id);
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogInformation("Successfully cleaned up {Count} expired reservations", expiredReservations.Count);
    }
}
