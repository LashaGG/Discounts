using Discounts.Application.Models;

namespace Discounts.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<DiscountModel>> GetActiveDiscountsAsync(CancellationToken ct = default);
    Task<IEnumerable<DiscountModel>> GetDiscountsByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<IEnumerable<DiscountModel>> SearchDiscountsAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<DiscountModel>> FilterDiscountsAsync(DiscountFilterModel filter, CancellationToken ct = default);
    Task<DiscountModel?> GetDiscountDetailsAsync(int discountId, CancellationToken ct = default);

    Task<ReservationResultModel> ReserveCouponAsync(int discountId, string customerId, CancellationToken ct = default);
    Task<PurchaseResultModel> PurchaseCouponAsync(int discountId, string customerId, CancellationToken ct = default);
    Task<bool> CancelReservationAsync(int couponId, string customerId, CancellationToken ct = default);

    Task<IEnumerable<CouponModel>> GetMyActiveCouponsAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<CouponModel>> GetMyUsedCouponsAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<CouponModel>> GetMyExpiredCouponsAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<CouponModel>> GetAllMyCouponsAsync(string customerId, CancellationToken ct = default);
    Task<CouponModel?> GetCouponDetailsAsync(int couponId, string customerId, CancellationToken ct = default);
    Task<bool> MarkCouponAsUsedAsync(int couponId, string customerId, CancellationToken ct = default);
}
