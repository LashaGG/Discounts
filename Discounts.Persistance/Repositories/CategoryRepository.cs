using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Categories.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Category>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return category;
    }

    public Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Update(category);
        return _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _context.Categories.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return _context.Categories.AnyAsync(c => c.Id == id, ct);
    }
}
