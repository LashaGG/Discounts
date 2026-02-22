using Discounts.Application.HealthChecks;
using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;

namespace Discounts.Infrastructure.BackgroundServices;

public class ReservationCleanupService : BackgroundService
{
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerHealthRegistry _healthRegistry;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(3);

    public ReservationCleanupService(
        ILogger<ReservationCleanupService> logger,
        IServiceProvider serviceProvider,
        WorkerHealthRegistry healthRegistry)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _healthRegistry = healthRegistry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _healthRegistry.ReportHealthy(nameof(ReservationCleanupService));
                await CleanupExpiredReservationsAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired reservations");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Reservation Cleanup Service stopped");
    }

    private async Task CleanupExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var couponRepository = scope.ServiceProvider.GetRequiredService<ICouponRepository>();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();

        // Get reservation duration from settings
        var reservationMinutes = await settingsRepo.GetIntValueAsync(
            SettingsKeys.ReservationDuration, 30, ct).ConfigureAwait(false);

        var expirationThreshold = DateTime.UtcNow.AddMinutes(-reservationMinutes);

        // Find all expired reservations
        var expiredReservations = await couponRepository
            .GetExpiredReservationsAsync(expirationThreshold, ct).ConfigureAwait(false);

        if (expiredReservations.Count == 0)
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("Found {Count} expired reservations", expiredReservations.Count);

        foreach (var coupon in expiredReservations)
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

        await couponRepository.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Successfully cleaned up {Count} expired reservations", expiredReservations.Count);
    }
}
