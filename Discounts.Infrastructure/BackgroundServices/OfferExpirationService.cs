using Discounts.Application.HealthChecks;
using Discounts.Application.Interfaces;
using Discounts.Domain.Enums;

namespace Discounts.Infrastructure.BackgroundServices;

public class OfferExpirationService : BackgroundService
{
    private readonly ILogger<OfferExpirationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerHealthRegistry _healthRegistry;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

    public OfferExpirationService(
        ILogger<OfferExpirationService> logger,
        IServiceProvider serviceProvider,
        WorkerHealthRegistry healthRegistry)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _healthRegistry = healthRegistry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Offer Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _healthRegistry.ReportHealthy(nameof(OfferExpirationService));
                await ExpireOffersAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring offers");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Offer Expiration Service stopped");
    }

    private async Task ExpireOffersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var discountRepository = scope.ServiceProvider.GetRequiredService<IDiscountRepository>();

        var now = DateTime.UtcNow;

        // Find active discounts whose validity period has passed
        var expiredDiscounts = await discountRepository
            .GetExpiredActiveWithCouponsAsync(now, ct).ConfigureAwait(false);

        if (expiredDiscounts.Count == 0)
        {
            _logger.LogDebug("No expired offers found");
            return;
        }

        _logger.LogInformation("Found {Count} expired offers to process", expiredDiscounts.Count);

        foreach (var discount in expiredDiscounts)
        {
            discount.Status = DiscountStatus.Expired;
            discount.LastModifiedAt = now;

            // Expire any remaining available coupons for this discount
            foreach (var coupon in discount.Coupons
                         .Where(c => c.Status == CouponStatus.Available))
            {
                coupon.Status = CouponStatus.Expired;
            }

            // Cancel active reservations so reserved coupons are not left dangling
            foreach (var coupon in discount.Coupons
                         .Where(c => c.Status == CouponStatus.Reserved))
            {
                coupon.Status = CouponStatus.Expired;
                coupon.CustomerId = null;
                coupon.ReservedAt = null;
            }

            _logger.LogInformation(
                "Expired offer - Discount: {DiscountId}, Title: {Title}",
                discount.Id, discount.Title);
        }

        await discountRepository.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Successfully expired {Count} offers", expiredDiscounts.Count);
    }
}
