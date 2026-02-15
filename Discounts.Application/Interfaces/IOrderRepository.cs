using Discounts.Domain.Entities.Business;

namespace Discounts.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByDiscountIdAsync(int discountId, CancellationToken ct = default);
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task<string> GenerateOrderNumberAsync(CancellationToken ct = default);

    // Dashboard queries
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default);
    Task<decimal> GetCompletedRevenueSumAsync(CancellationToken ct = default);
}
