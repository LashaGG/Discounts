using Discounts.Application.Models;
using Discounts.Domain.Enums;

namespace Discounts.Application.Interfaces;

public interface IMerchantService
{
    Task<MerchantDashboardModel> GetDashboardAsync(string merchantId, CancellationToken ct = default);
    Task<IEnumerable<DiscountModel>> GetMerchantDiscountsAsync(string merchantId, CancellationToken ct = default);
    Task<IEnumerable<DiscountModel>> GetMerchantDiscountsByStatusAsync(string merchantId, DiscountStatus status, CancellationToken ct = default);
    Task<DiscountModel?> GetDiscountByIdAsync(int discountId, string merchantId, CancellationToken ct = default);
    Task<DiscountModel> CreateDiscountAsync(CreateDiscountModel model, string merchantId, CancellationToken ct = default);
    Task<DiscountModel> UpdateDiscountAsync(UpdateDiscountModel model, string merchantId, CancellationToken ct = default);
    Task<bool> DeleteDiscountAsync(int discountId, string merchantId, CancellationToken ct = default);
    Task<IEnumerable<SalesHistoryModel>> GetSalesHistoryAsync(int discountId, string merchantId, CancellationToken ct = default);
    Task<bool> CanEditDiscountAsync(int discountId, string merchantId, CancellationToken ct = default);
}
