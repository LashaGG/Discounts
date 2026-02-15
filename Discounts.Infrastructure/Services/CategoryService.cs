using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Mapster;

namespace Discounts.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryModel>> GetActiveCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _categoryRepository.GetActiveAsync(ct).ConfigureAwait(false);
        return categories.Adapt<IEnumerable<CategoryModel>>();
    }
}
