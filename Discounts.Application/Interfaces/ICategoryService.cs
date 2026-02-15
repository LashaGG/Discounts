using Discounts.Application.Models;

namespace Discounts.Application.Interfaces;

/// <summary>
/// Provides read-only category operations for customer and merchant-facing features.
/// </summary>
public interface ICategoryService
{
    Task<IEnumerable<CategoryModel>> GetActiveCategoriesAsync(CancellationToken ct = default);
}
