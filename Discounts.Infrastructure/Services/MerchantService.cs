using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Enums;
using Mapster;

namespace Discounts.Infrastructure.Services;

public class MerchantService : IMerchantService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly int _editAllowedHours = 24;

    public MerchantService(
        IDiscountRepository discountRepository,
        ICouponRepository couponRepository,
        IOrderRepository orderRepository)
    {
        _discountRepository = discountRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
    }

    public async Task<MerchantDashboardModel> GetDashboardAsync(string merchantId, CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetByMerchantIdAsync(merchantId, ct).ConfigureAwait(false);
        var discountList = discounts.ToList();

        var dashboard = new MerchantDashboardModel
        {
            TotalDiscounts = discountList.Count,
            ActiveDiscounts = discountList.Count(d => d.Status == DiscountStatus.Active && d.IsActive),
            PendingDiscounts = discountList.Count(d => d.Status == DiscountStatus.Pending),
            ExpiredDiscounts = discountList.Count(d => d.IsExpired || d.Status == DiscountStatus.Expired),
            RecentDiscounts = discountList
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => d.Adapt<DiscountModel>())
                .ToList()
        };

        foreach (var discount in discountList.Where(d => d.SoldCoupons > 0))
        {
            dashboard.TotalSales += discount.SoldCoupons;
            dashboard.TotalRevenue += discount.DiscountedPrice * discount.SoldCoupons;

            dashboard.SalesStats.Add(new SalesStatModel
            {
                DiscountTitle = discount.Title,
                CouponsSold = discount.SoldCoupons,
                Revenue = discount.DiscountedPrice * discount.SoldCoupons
            });
        }

        return dashboard;
    }

    public async Task<IEnumerable<DiscountModel>> GetMerchantDiscountsAsync(string merchantId, CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetByMerchantIdAsync(merchantId, ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<IEnumerable<DiscountModel>> GetMerchantDiscountsByStatusAsync(
        string merchantId,
        DiscountStatus status,
        CancellationToken ct = default)
    {
        var discounts = await _discountRepository.GetByMerchantIdAsync(merchantId, ct).ConfigureAwait(false);
        return discounts
            .Where(d => d.Status == status)
            .Adapt<IEnumerable<DiscountModel>>();
    }

    public async Task<DiscountModel?> GetDiscountByIdAsync(int discountId, string merchantId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null || discount.MerchantId != merchantId)
            return null;

        return discount.Adapt<DiscountModel>();
    }

    public async Task<DiscountModel> CreateDiscountAsync(CreateDiscountModel model, string merchantId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.DiscountedPrice >= model.OriginalPrice)
            throw new InvalidOperationException("ფასდაკლებული ფასი უნდა იყოს ნაკლები ორიგინალურ ფასზე");

        if (model.ValidFrom >= model.ValidTo)
            throw new InvalidOperationException("დასრულების თარიღი უნდა იყოს დაწყების თარიღზე გვიან");

        if (model.ValidFrom < DateTime.UtcNow.AddHours(-1))
            throw new InvalidOperationException("დაწყების თარიღი არ შეიძლება იყოს წარსულში");

        var discount = model.Adapt<Discount>();
        discount.MerchantId = merchantId;

        var created = await _discountRepository.CreateAsync(discount, ct).ConfigureAwait(false);

        var coupons = new List<Coupon>();
        for (var i = 0; i < model.TotalCoupons; i++)
        {
            var code = await _couponRepository.GenerateUniqueCouponCodeAsync(ct).ConfigureAwait(false);
            coupons.Add(new Coupon
            {
                Code = code,
                DiscountId = created.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _couponRepository.CreateBulkAsync(coupons, ct).ConfigureAwait(false);

        return created.Adapt<DiscountModel>();
    }

    public async Task<DiscountModel> UpdateDiscountAsync(UpdateDiscountModel model, string merchantId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.DiscountedPrice >= model.OriginalPrice)
            throw new InvalidOperationException("ფასდაკლებული ფასი უნდა იყოს ნაკლები ორიგინალურ ფასზე");

        if (model.ValidFrom >= model.ValidTo)
            throw new InvalidOperationException("დასრულების თარიღი უნდა იყოს დაწყების თარიღზე გვიან");

        var discount = await _discountRepository.GetByIdAsync(model.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("ფასდაკლება ვერ მოიძებნა");

        if (discount.MerchantId != merchantId)
            throw new UnauthorizedAccessException("თქვენ არ გაქვთ ამ ფასდაკლების რედაქტირების უფლება");

        if (!discount.CanBeEdited(_editAllowedHours))
            throw new InvalidOperationException($"ფასდაკლების რედაქტირება შესაძლებელია მხოლოდ {_editAllowedHours} საათის განმავლობაში");

        discount.Title = model.Title;
        discount.Description = model.Description;
        discount.ImageUrl = model.ImageUrl;
        discount.OriginalPrice = model.OriginalPrice;
        discount.DiscountedPrice = model.DiscountedPrice;
        discount.ValidFrom = model.ValidFrom;
        discount.ValidTo = model.ValidTo;
        discount.CategoryId = model.CategoryId;

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);

        return discount.Adapt<DiscountModel>();
    }

    public async Task<bool> DeleteDiscountAsync(int discountId, string merchantId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null || discount.MerchantId != merchantId)
            return false;

        if (discount.SoldCoupons > 0)
            throw new InvalidOperationException("Cannot delete discount with sold coupons");

        await _discountRepository.DeleteAsync(discountId, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IEnumerable<SalesHistoryModel>> GetSalesHistoryAsync(int discountId, string merchantId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null || discount.MerchantId != merchantId)
            return Enumerable.Empty<SalesHistoryModel>();

        var orders = await _orderRepository.GetByDiscountIdAsync(discountId, ct).ConfigureAwait(false);

        return orders.Select(o => new SalesHistoryModel
        {
            OrderId = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = $"{o.Customer?.FirstName} {o.Customer?.LastName}".Trim(),
            CouponCount = o.Quantity,
            TotalAmount = o.TotalAmount,
            OrderDate = o.CreatedAt,
            Status = o.Status.ToString()
        });
    }

    public async Task<bool> CanEditDiscountAsync(int discountId, string merchantId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null || discount.MerchantId != merchantId)
            return false;

        return discount.CanBeEdited(_editAllowedHours);
    }
}
