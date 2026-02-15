using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Mapster;

namespace Discounts.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ISystemSettingsRepository _settingsRepository;

    public CustomerService(
        IDiscountRepository discountRepository,
        ICouponRepository couponRepository,
        ISystemSettingsRepository settingsRepository)
    {
        _discountRepository = discountRepository;
        _couponRepository = couponRepository;
        _settingsRepository = settingsRepository;
    }

    // Browse & Search
    public async Task<IEnumerable<DiscountModel>> GetActiveDiscountsAsync(CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetActiveWithDetailsAsync(ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
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
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null)
            return new ReservationResultModel { Success = false, Message = "ფასდაკლება ვერ მოიძებნა" };

        if (discount.Status != DiscountStatus.Active)
            return new ReservationResultModel { Success = false, Message = "ფასდაკლება აღარ არის აქტიური" };

        if (discount.AvailableCoupons <= 0)
            return new ReservationResultModel { Success = false, Message = "კუპონები ამოიწურა" };

        var existingReservation = await _couponRepository
            .GetReservedByDiscountAndCustomerAsync(discountId, customerId, ct).ConfigureAwait(false);

        if (existingReservation != null)
            return new ReservationResultModel { Success = false, Message = "თქვენ უკვე გაქვთ ამ ფასდაკლების ჯავშანი" };

        var availableCoupon = await _couponRepository
            .GetFirstAvailableByDiscountAsync(discountId, ct).ConfigureAwait(false);

        if (availableCoupon == null)
            return new ReservationResultModel { Success = false, Message = "ხელმისაწვდომი კუპონი ვერ მოიძებნა" };

        var reservationMinutes = await _settingsRepository.GetIntValueAsync(
            SettingsKeys.ReservationDuration, 30, ct).ConfigureAwait(false);

        availableCoupon.Status = CouponStatus.Reserved;
        availableCoupon.CustomerId = customerId;
        availableCoupon.ReservedAt = DateTime.UtcNow;

        discount.AvailableCoupons--;

        // Both entities are tracked by the same scoped DbContext; SaveChanges persists all changes
        await _couponRepository.SaveChangesAsync(ct).ConfigureAwait(false);

        return new ReservationResultModel
        {
            Success = true,
            Message = "კუპონი წარმატებით დაჯავშნილია",
            CouponId = availableCoupon.Id,
            ExpiresAt = availableCoupon.ReservedAt.Value.AddMinutes(reservationMinutes),
            ReservationMinutes = reservationMinutes
        };
    }

    public async Task<PurchaseResultModel> PurchaseCouponAsync(int discountId, string customerId, CancellationToken ct = default)
    {
        var reservedCoupon = await _couponRepository
            .GetReservedByDiscountAndCustomerWithDiscountAsync(discountId, customerId, ct).ConfigureAwait(false);

        var coupon = reservedCoupon;

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
            return new PurchaseResultModel { Success = false, Message = "კუპონი ვერ მოიძებნა" };

        coupon.Status = CouponStatus.Purchased;
        coupon.PurchasedAt = DateTime.UtcNow;

        await _couponRepository.UpdateAsync(coupon, ct).ConfigureAwait(false);

        return new PurchaseResultModel
        {
            Success = true,
            Message = "კუპონი წარმატებით შეძენილია",
            CouponId = coupon.Id,
            CouponCode = coupon.Code,
            Amount = coupon.Discount.DiscountedPrice
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
