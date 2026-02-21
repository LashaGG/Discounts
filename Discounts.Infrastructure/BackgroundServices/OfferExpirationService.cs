using Discounts.Domain.Enums;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Infrastructure.BackgroundServices;

public class OfferExpirationService : BackgroundService
{
    private readonly ILogger<OfferExpirationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

    public OfferExpirationService(
        ILogger<OfferExpirationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Offer Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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

    private async Task ExpireOffersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Find active discounts whose validity period has passed
        var expiredDiscounts = await context.Discounts
            .Include(d => d.Coupons)
            .Where(d => d.Status == DiscountStatus.Active && d.ValidTo < now)
            .ToListAsync(stoppingToken).ConfigureAwait(false);

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

            _logger.LogInformation(
                "Expired offer - Discount: {DiscountId}, Title: {Title}",
                discount.Id, discount.Title);
        }
            
        await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully expired {Count} offers", expiredDiscounts.Count);
    }
}
