using Discounts.Domain.Entities.Business;

namespace Discounts.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Category>> GetActiveAsync(CancellationToken ct = default);
    Task<Category> CreateAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}
