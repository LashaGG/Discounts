using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Enums;

namespace Discounts.Application.Interfaces;

public interface IDiscountRepository
{
    Task<Discount?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetByMerchantIdAsync(string merchantId, CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetByStatusAsync(DiscountStatus status, CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetActiveMerchantDiscountsAsync(string merchantId, CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetPendingApprovalAsync(CancellationToken ct = default);
    Task<Discount> CreateAsync(Discount discount, CancellationToken ct = default);
    Task UpdateAsync(Discount discount, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    // Active discount queries (with Category/Merchant includes)
    Task<IEnumerable<Discount>> GetActiveWithDetailsAsync(CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetActiveByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<IEnumerable<Discount>> SearchActiveAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<Discount>> FilterActiveAsync(DiscountFilterModel filter, CancellationToken ct = default);
    Task<Discount?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);

    // Dashboard queries
    Task<IEnumerable<Discount>> GetRecentWithDetailsAsync(int count, CancellationToken ct = default);
    Task<IEnumerable<Discount>> GetPendingWithDetailsAsync(CancellationToken ct = default);
}
