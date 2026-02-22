using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Enums;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly ApplicationDbContext _context;

    public CouponRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Coupon?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _context.Coupons
            .Include(c => c.Discount)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return _context.Coupons
            .Include(c => c.Discount)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Code == code, ct);
    }

    public Task<Coupon?> GetByCodeWithDiscountAsync(string code, CancellationToken ct = default)
    {
        return _context.Coupons
            .Include(c => c.Discount)
            .FirstOrDefaultAsync(c => c.Code == code, ct);
    }

    public async Task<IEnumerable<Coupon>> GetByDiscountIdAsync(int discountId, CancellationToken ct = default)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Customer)
            .Where(c => c.DiscountId == discountId)
            .OrderByDescending(c => c.PurchasedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Coupon>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
            .ThenInclude(d => d.Category)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.PurchasedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Coupon>> CreateBulkAsync(IEnumerable<Coupon> coupons, CancellationToken ct = default)
    {
        await _context.Coupons.AddRangeAsync(coupons, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return coupons;
    }

    public Task UpdateAsync(Coupon coupon, CancellationToken ct = default)
    {
        _context.Coupons.Update(coupon);
        return _context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateUniqueCouponCodeAsync(CancellationToken ct = default)
    {
        string code;
        bool exists;

        do
        {
            code = $"CPN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
            exists = await _context.Coupons.AnyAsync(c => c.Code == code, ct).ConfigureAwait(false);
        } while (exists);

        return code;
    }

    public Task<Coupon?> GetReservedByDiscountAndCustomerAsync(int discountId, string customerId, CancellationToken ct = default)
    {
        return _context.Coupons
            .FirstOrDefaultAsync(c => c.DiscountId == discountId &&
                                     c.CustomerId == customerId &&
                                     c.Status == CouponStatus.Reserved, ct);
    }

    public Task<Coupon?> GetFirstAvailableByDiscountAsync(int discountId, CancellationToken ct = default)
    {
        return _context.Coupons
            .FirstOrDefaultAsync(c => c.DiscountId == discountId &&
                                     c.Status == CouponStatus.Available, ct);
    }

    public Task<Coupon?> GetReservedByIdAndCustomerWithDiscountAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        return _context.Coupons
            .Include(c => c.Discount)
            .FirstOrDefaultAsync(c => c.Id == couponId &&
                                     c.CustomerId == customerId &&
                                     c.Status == CouponStatus.Reserved, ct);
    }

    public Task<Coupon?> GetReservedByDiscountAndCustomerWithDiscountAsync(int discountId, string customerId, CancellationToken ct = default)
    {
        return _context.Coupons
            .Include(c => c.Discount)
            .FirstOrDefaultAsync(c => c.DiscountId == discountId &&
                                     c.CustomerId == customerId &&
                                     c.Status == CouponStatus.Reserved, ct);
    }

    public Task<Coupon?> GetPurchasedByIdAndCustomerAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        return _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == couponId &&
                                     c.CustomerId == customerId &&
                                     c.Status == CouponStatus.Purchased, ct);
    }

    public async Task<IEnumerable<Coupon>> GetActivePurchasedByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
                .ThenInclude(d => d.Category)
            .Include(c => c.Discount)
                .ThenInclude(d => d.Merchant)
            .Where(c => c.CustomerId == customerId &&
                       c.Status == CouponStatus.Purchased &&
                       c.Discount.ValidTo >= now)
            .OrderByDescending(c => c.PurchasedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Coupon>> GetUsedByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
                .ThenInclude(d => d.Category)
            .Include(c => c.Discount)
                .ThenInclude(d => d.Merchant)
            .Where(c => c.CustomerId == customerId &&
                       c.Status == CouponStatus.Used)
            .OrderByDescending(c => c.UsedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Coupon>> GetExpiredPurchasedByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
                .ThenInclude(d => d.Category)
            .Include(c => c.Discount)
                .ThenInclude(d => d.Merchant)
            .Where(c => c.CustomerId == customerId &&
                       c.Status == CouponStatus.Purchased &&
                       c.Discount.ValidTo < now)
            .OrderByDescending(c => c.PurchasedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Coupon>> GetAllPurchasedOrUsedByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
                .ThenInclude(d => d.Category)
            .Include(c => c.Discount)
                .ThenInclude(d => d.Merchant)
            .Where(c => c.CustomerId == customerId &&
                       (c.Status == CouponStatus.Purchased ||
                        c.Status == CouponStatus.Used))
            .OrderByDescending(c => c.PurchasedAt ?? c.UsedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<Coupon?> GetByIdAndCustomerWithDetailsAsync(int couponId, string customerId, CancellationToken ct = default)
    {
        return _context.Coupons
            .AsNoTracking()
            .Include(c => c.Discount)
                .ThenInclude(d => d.Category)
            .Include(c => c.Discount)
                .ThenInclude(d => d.Merchant)
            .FirstOrDefaultAsync(c => c.Id == couponId &&
                                     c.CustomerId == customerId, ct);
    }

    public async Task<IReadOnlyList<Coupon>> GetExpiredReservationsAsync(DateTime expirationThreshold, CancellationToken ct = default)
    {
        return await _context.Coupons
            .Include(c => c.Discount)
            .Where(c => c.Status == CouponStatus.Reserved &&
                       c.ReservedAt.HasValue &&
                       c.ReservedAt.Value < expirationThreshold)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
