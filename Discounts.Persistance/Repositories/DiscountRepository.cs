using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Enums;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly ApplicationDbContext _context;

    public DiscountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Discount?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _context.Discounts
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Include(d => d.ApprovedByAdmin)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IEnumerable<Discount>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Discounts
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetByMerchantIdAsync(string merchantId, CancellationToken ct = default)
    {
        return await _context.Discounts
            .Include(d => d.Category)
            .Where(d => d.MerchantId == merchantId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetByStatusAsync(DiscountStatus status, CancellationToken ct = default)
    {
        return await _context.Discounts
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetActiveMerchantDiscountsAsync(string merchantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Include(d => d.Category)
            .Where(d => d.MerchantId == merchantId &&
                       d.Status == DiscountStatus.Active &&
                       d.ValidFrom <= now &&
                       d.ValidTo >= now &&
                       d.AvailableCoupons > 0)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetPendingApprovalAsync(CancellationToken ct = default)
    {
        return await _context.Discounts
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == DiscountStatus.Pending)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Discount> CreateAsync(Discount discount, CancellationToken ct = default)
    {
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return discount;
    }

    public Task UpdateAsync(Discount discount, CancellationToken ct = default)
    {
        discount.LastModifiedAt = DateTime.UtcNow;
        _context.Discounts.Update(discount);
        return _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var discount = await _context.Discounts.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (discount != null)
        {
            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return _context.Discounts.AnyAsync(d => d.Id == id, ct);
    }

    public async Task<IEnumerable<Discount>> GetActiveWithDetailsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == DiscountStatus.Active &&
                       d.ValidFrom <= now &&
                       d.ValidTo >= now)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetActiveByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.CategoryId == categoryId &&
                       d.Status == DiscountStatus.Active &&
                       d.ValidFrom <= now &&
                       d.ValidTo >= now)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> SearchActiveAsync(string searchTerm, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == DiscountStatus.Active &&
                       d.ValidFrom <= now &&
                       d.ValidTo >= now &&
                       (d.Title.Contains(searchTerm) ||
                        d.Description.Contains(searchTerm) ||
                        d.Category.Name.Contains(searchTerm)))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> FilterActiveAsync(DiscountFilterModel filter, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == DiscountStatus.Active &&
                       d.ValidFrom <= now &&
                       d.ValidTo >= now);

        if (filter.CategoryId.HasValue)
            query = query.Where(d => d.CategoryId == filter.CategoryId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(d => d.DiscountedPrice >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(d => d.DiscountedPrice <= filter.MaxPrice.Value);

        if (filter.MinDiscount.HasValue)
            query = query.Where(d => d.DiscountPercentage >= filter.MinDiscount.Value);

        if (filter.MaxDiscount.HasValue)
            query = query.Where(d => d.DiscountPercentage <= filter.MaxDiscount.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            query = query.Where(d => d.Title.Contains(filter.SearchTerm) ||
                                    d.Description.Contains(filter.SearchTerm) ||
                                    d.Category.Name.Contains(filter.SearchTerm));
        }

        query = filter.SortBy?.ToLower() switch
        {
            "price" => filter.SortDescending
                ? query.OrderByDescending(d => d.DiscountedPrice)
                : query.OrderBy(d => d.DiscountedPrice),
            "discount" => filter.SortDescending
                ? query.OrderByDescending(d => d.DiscountPercentage)
                : query.OrderBy(d => d.DiscountPercentage),
            "date" => filter.SortDescending
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.OrderBy(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<Discount?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IEnumerable<Discount>> GetRecentWithDetailsAsync(int count, CancellationToken ct = default)
    {
        return await _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .OrderByDescending(d => d.CreatedAt)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Discount>> GetPendingWithDetailsAsync(CancellationToken ct = default)
    {
        return await _context.Discounts
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Merchant)
            .Where(d => d.Status == DiscountStatus.Pending)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
