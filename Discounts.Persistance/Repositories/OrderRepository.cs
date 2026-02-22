using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Enums;
using Discounts.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Discount)
            .Include(o => o.Coupons)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Discount)
            .Include(o => o.Coupons)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Discount)
            .ThenInclude(d => d.Category)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Order>> GetByDiscountIdAsync(int discountId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Coupons)
            .Where(o => o.DiscountId == discountId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return order;
    }

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        return _context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken ct = default)
    {
        string orderNumber;
        bool exists;

        do
        {
            orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            exists = await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber, ct).ConfigureAwait(false);
        } while (exists);

        return orderNumber;
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<decimal> GetCompletedRevenueSumAsync(CancellationToken ct = default)
    {
        return _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount, ct);
    }
}
