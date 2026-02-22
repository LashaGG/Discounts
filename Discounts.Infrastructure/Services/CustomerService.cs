using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Application;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Discounts.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IStringLocalizer<ServiceMessages> _localizer;

    public CustomerService(
        IDiscountRepository discountRepository,
        ICouponRepository couponRepository,
        IOrderRepository orderRepository,
        ISystemSettingsRepository settingsRepository,
        IStringLocalizer<ServiceMessages> localizer)
    {
        _discountRepository = discountRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
        _settingsRepository = settingsRepository;
        _localizer = localizer;
    }

    // Browse & Search
    public async Task<IEnumerable<DiscountModel>> GetActiveDiscountsAsync(CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetActiveWithDetailsAsync(ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<PagedResult<DiscountModel>> GetActiveDiscountsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _discountRepository.GetActivePagedAsync(page, pageSize, ct).ConfigureAwait(false);

        return new PagedResult<DiscountModel>
        {
            Items = items.Adapt<IReadOnlyList<DiscountModel>>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IEnumerable<DiscountModel>> GetDiscountsByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetActiveByCategoryAsync(categoryId, ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<IEnumerable<DiscountModel>> SearchDiscountsAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetActiveDiscountsAsync(ct).ConfigureAwait(false);

        var discounts = await _discountRepository.SearchActiveAsync(searchTerm, ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<IEnumerable<DiscountModel>> FilterDiscountsAsync(DiscountFilterModel filter, CancellationToken ct = default)
    {
        var discounts = await _discountRepository.FilterActiveAsync(filter, ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<DiscountModel?> GetDiscountDetailsAsync(int discountId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdWithDetailsAsync(discountId, ct).ConfigureAwait(false);
        return discount?.Adapt<DiscountModel>();
    }

    // Booking & Purchase
    public async Task<ReservationResultModel> ReserveCouponAsync(int discountId, string customerId, CancellationToken ct = default)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

            if (discount == null)
                return new ReservationResultModel { Success = false, Message = _localizer["Service_DiscountNotFound"] };

            if (discount.Status != DiscountStatus.Active)
                return new ReservationResultModel { Success = false, Message = _localizer["Service_DiscountNoLongerActive"] };

            if (discount.AvailableCoupons <= 0)
                return new ReservationResultModel { Success = false, Message = _localizer["Service_CouponsSoldOut"] };

            // Check reservation duration to detect logically expired reservations
            var reservationMinutes = await _settingsRepository.GetIntValueAsync(
                SettingsKeys.ReservationDuration, 30, ct).ConfigureAwait(false);

            var existingReservation = await _couponRepository
                .GetReservedByDiscountAndCustomerAsync(discountId, customerId, ct).ConfigureAwait(false);

            if (existingReservation != null)
            {
                // If the reservation has expired but the background cleanup hasn't run yet, release it inline
                var isExpired = existingReservation.ReservedAt.HasValue
                                && DateTime.UtcNow - existingReservation.ReservedAt.Value > TimeSpan.FromMinutes(reservationMinutes);

                if (isExpired)
                {
                    existingReservation.Status = CouponStatus.Available;
                    existingReservation.CustomerId = null;
                    existingReservation.ReservedAt = null;
                    discount.AvailableCoupons++;
                    await _couponRepository.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    return new ReservationResultModel { Success = false, Message = _localizer["Service_AlreadyReserved"] };
                }
            }

            var availableCoupon = await _couponRepository
                .GetFirstAvailableByDiscountAsync(discountId, ct).ConfigureAwait(false);

            if (availableCoupon == null)
                return new ReservationResultModel { Success = false, Message = _localizer["Service_NoCouponAvailable"] };

            availableCoupon.Status = CouponStatus.Reserved;
            availableCoupon.CustomerId = customerId;
            availableCoupon.ReservedAt = DateTime.UtcNow;

            discount.AvailableCoupons--;

            try
            {
                await _couponRepository.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                // Another request modified the Discount row concurrently; retry with fresh data
                continue;
            }

            return new ReservationResultModel
            {
                Success = true,
                Message = _localizer["Service_CouponReservedSuccessfully"],
                CouponId = availableCoupon.Id,
                ExpiresAt = availableCoupon.ReservedAt.Value.AddMinutes(reservationMinutes),
                ReservationMinutes = reservationMinutes
            };
        }

        return new ReservationResultModel { Success = false, Message = _localizer["Service_CouponsSoldOut"] };
    }

    public async Task<PurchaseResultModel> PurchaseCouponAsync(int discountId, string customerId, CancellationToken ct = default)
    {
        var reservedCoupon = await _couponRepository
            .GetReservedByDiscountAndCustomerWithDiscountAsync(discountId, customerId, ct).ConfigureAwait(false);

        var coupon = reservedCoupon;
        var wasAlreadyReserved = coupon != null;

        if (coupon == null)
        {
            var reservationResult = await ReserveCouponAsync(discountId, customerId, ct).ConfigureAwait(false);
            if (!reservationResult.Success || !reservationResult.CouponId.HasValue)
                return new PurchaseResultModel
                {
                    Success = false,
                    Message = reservationResult.Message
                };

            coupon = await _couponRepository.GetByIdAsync(reservationResult.CouponId.Value, ct).ConfigureAwait(false);
        }

        if (coupon == null)
            return new PurchaseResultModel { Success = false, Message = _localizer["Service_CouponNotFound"] };

        // Ensure the Discount navigation is loaded for pricing
        var discount = coupon.Discount
            ?? await _discountRepository.GetByIdAsync(coupon.DiscountId, ct).ConfigureAwait(false);

        if (discount == null)
            return new PurchaseResultModel { Success = false, Message = _localizer["Service_DiscountNotFound"] };

        var orderNumber = await _orderRepository.GenerateOrderNumberAsync(ct).ConfigureAwait(false);

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            DiscountId = coupon.DiscountId,
            TotalAmount = discount.DiscountedPrice,
            Quantity = 1,
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        var createdOrder = await _orderRepository.CreateAsync(order, ct).ConfigureAwait(false);

        coupon.Status = CouponStatus.Purchased;
        coupon.PurchasedAt = DateTime.UtcNow;
        coupon.OrderId = createdOrder.Id;

        await _couponRepository.UpdateAsync(coupon, ct).ConfigureAwait(false);

        return new PurchaseResultModel
        {
            Success = true,
            Message = _localizer["Service_CouponPurchasedSuccessfully"],
            CouponId = coupon.Id,
            CouponCode = coupon.Code,
            Amount = discount.DiscountedPrice
        };
    }

    public async Task<bool> CancelReservationAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        var coupon = await _couponRepository
            .GetReservedByIdAndCustomerWithDiscountAsync(couponId, customerId, ct).ConfigureAwait(false);

        if (coupon == null)
            return false;

        coupon.Status = CouponStatus.Available;
        coupon.CustomerId = null;
        coupon.ReservedAt = null;

        coupon.Discount.AvailableCoupons++;

        await _couponRepository.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    // My Coupons
    public async Task<IEnumerable<CouponModel>> GetMyActiveCouponsAsync(string customerId, CancellationToken ct = default)
    {
        var coupons = await _couponRepository.GetActivePurchasedByCustomerAsync(customerId, ct).ConfigureAwait(false);
        return coupons.Adapt<IEnumerable<CouponModel>>();
    }

    public async Task<IEnumerable<CouponModel>> GetMyUsedCouponsAsync(string customerId, CancellationToken ct = default)
    {
        var coupons = await _couponRepository.GetUsedByCustomerAsync(customerId, ct).ConfigureAwait(false);
        return coupons.Adapt<IEnumerable<CouponModel>>();
    }

    public async Task<IEnumerable<CouponModel>> GetMyExpiredCouponsAsync(string customerId, CancellationToken ct = default)
    {
        var coupons = await _couponRepository.GetExpiredPurchasedByCustomerAsync(customerId, ct).ConfigureAwait(false);
        return coupons.Adapt<IEnumerable<CouponModel>>();
    }

    public async Task<IEnumerable<CouponModel>> GetAllMyCouponsAsync(string customerId, CancellationToken ct = default)
    {
        var coupons = await _couponRepository.GetAllPurchasedOrUsedByCustomerAsync(customerId, ct).ConfigureAwait(false);
        return coupons.Adapt<IEnumerable<CouponModel>>();
    }

    public async Task<CouponModel?> GetCouponDetailsAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        var coupon = await _couponRepository
            .GetByIdAndCustomerWithDetailsAsync(couponId, customerId, ct).ConfigureAwait(false);

        return coupon?.Adapt<CouponModel>();
    }

    public async Task<bool> MarkCouponAsUsedAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        var coupon = await _couponRepository
            .GetPurchasedByIdAndCustomerAsync(couponId, customerId, ct).ConfigureAwait(false);

        if (coupon == null)
            return false;

        coupon.Status = CouponStatus.Used;
        coupon.UsedAt = DateTime.UtcNow;

        await _couponRepository.UpdateAsync(coupon, ct).ConfigureAwait(false);
        return true;
    }
}
