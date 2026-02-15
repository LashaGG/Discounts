using Discounts.Domain.Entities.Business;

namespace Discounts.Application.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> GetByDiscountIdAsync(int discountId, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> CreateBulkAsync(IEnumerable<Coupon> coupons, CancellationToken ct = default);
    Task UpdateAsync(Coupon coupon, CancellationToken ct = default);
    Task<string> GenerateUniqueCouponCodeAsync(CancellationToken ct = default);

    // Reservation & purchase queries
    Task<Coupon?> GetReservedByDiscountAndCustomerAsync(int discountId, string customerId, CancellationToken ct = default);
    Task<Coupon?> GetFirstAvailableByDiscountAsync(int discountId, CancellationToken ct = default);
    Task<Coupon?> GetReservedByIdAndCustomerWithDiscountAsync(int couponId, string customerId, CancellationToken ct = default);
    Task<Coupon?> GetReservedByDiscountAndCustomerWithDiscountAsync(int discountId, string customerId, CancellationToken ct = default);
    Task<Coupon?> GetPurchasedByIdAndCustomerAsync(int couponId, string customerId, CancellationToken ct = default);

    // Customer coupon listing queries (with Discount/Category/Merchant includes)
    Task<IEnumerable<Coupon>> GetActivePurchasedByCustomerAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> GetUsedByCustomerAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> GetExpiredPurchasedByCustomerAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<Coupon>> GetAllPurchasedOrUsedByCustomerAsync(string customerId, CancellationToken ct = default);
    Task<Coupon?> GetByIdAndCustomerWithDetailsAsync(int couponId, string customerId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
