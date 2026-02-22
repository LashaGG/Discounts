using Discounts.Application;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Enums;
using Mapster;
using Microsoft.Extensions.Localization;

namespace Discounts.Infrastructure.Services;

public class MerchantService : IMerchantService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IStringLocalizer<ServiceMessages> _localizer;
    private const int DefaultEditAllowedHours = 24;

    public MerchantService(
        IDiscountRepository discountRepository,
        ICouponRepository couponRepository,
        IOrderRepository orderRepository,
        ISystemSettingsRepository settingsRepository,
        IStringLocalizer<ServiceMessages> localizer)
    {
        _discountRepository = discountRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
        _settingsRepository = settingsRepository;
        _localizer = localizer;
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
        var discounts = await _discountRepository.GetByMerchantIdAndStatusAsync(merchantId, status, ct).ConfigureAwait(false);
        return discounts.Adapt<IEnumerable<DiscountModel>>();
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
            throw new InvalidOperationException(_localizer["Service_DiscountedPriceMustBeLess"]);

        // Convert to UTC without mutating the caller's model
        var validFrom = model.ValidFrom.Kind != DateTimeKind.Utc
            ? model.ValidFrom.ToUniversalTime()
            : model.ValidFrom;
        var validTo = model.ValidTo.Kind != DateTimeKind.Utc
            ? model.ValidTo.ToUniversalTime()
            : model.ValidTo;

        if (validFrom >= validTo)
            throw new InvalidOperationException(_localizer["Service_EndDateMustBeAfterStart"]);

        if (validFrom < DateTime.UtcNow.AddHours(-1))
            throw new InvalidOperationException(_localizer["Service_StartDateCannotBeInPast"]);

        var discount = model.Adapt<Discount>();
        discount.MerchantId = merchantId;
        discount.ValidFrom = validFrom;
        discount.ValidTo = validTo;

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
            throw new InvalidOperationException(_localizer["Service_DiscountedPriceMustBeLess"]);

        // Convert to UTC without mutating the caller's model
        var validFrom = model.ValidFrom.Kind != DateTimeKind.Utc
            ? model.ValidFrom.ToUniversalTime()
            : model.ValidFrom;
        var validTo = model.ValidTo.Kind != DateTimeKind.Utc
            ? model.ValidTo.ToUniversalTime()
            : model.ValidTo;

        if (validFrom >= validTo)
            throw new InvalidOperationException(_localizer["Service_EndDateMustBeAfterStart"]);

        var discount = await _discountRepository.GetByIdAsync(model.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(_localizer["Service_DiscountNotFound"]);

        if (discount.MerchantId != merchantId)
            throw new UnauthorizedAccessException(_localizer["Service_NoPermissionToEdit"]);

        var editAllowedHours = await _settingsRepository.GetIntValueAsync(
            SettingsKeys.MerchantEditWindow, DefaultEditAllowedHours, ct).ConfigureAwait(false);

        if (!discount.CanBeEdited(editAllowedHours))
            throw new InvalidOperationException(_localizer["Service_EditWindowExpired", editAllowedHours]);

        discount.Title = model.Title;
        discount.Description = model.Description;
        discount.ImageUrl = model.ImageUrl;
        discount.OriginalPrice = model.OriginalPrice;
        discount.DiscountedPrice = model.DiscountedPrice;
        discount.ValidFrom = validFrom;
        discount.ValidTo = validTo;
        discount.CategoryId = model.CategoryId;

        // If the discount was Approved (Active), revert to Pending for re-approval
        if (discount.Status == DiscountStatus.Approved || discount.Status == DiscountStatus.Active)
        {
            discount.Status = DiscountStatus.Pending;
            discount.ApprovedByAdminId = null;
            discount.ApprovedAt = null;
            discount.RejectionReason = null;
        }

        // If it was Rejected, keep it as Pending so the merchant can resubmit
        if (discount.Status == DiscountStatus.Rejected)
        {
            discount.Status = DiscountStatus.Pending;
            discount.RejectionReason = null;
        }

        await _discountRepository.UpdateAsync(discount, ct).ConfigureAwait(false);

        return discount.Adapt<DiscountModel>();
    }

    public async Task<bool> DeleteDiscountAsync(int discountId, string merchantId, CancellationToken ct = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, ct).ConfigureAwait(false);

        if (discount == null || discount.MerchantId != merchantId)
            return false;

        if (discount.SoldCoupons > 0)
            throw new InvalidOperationException(_localizer["Service_CannotDeleteWithSoldCoupons"]);

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

        var editAllowedHours = await _settingsRepository.GetIntValueAsync(
            SettingsKeys.MerchantEditWindow, DefaultEditAllowedHours, ct).ConfigureAwait(false);

        return discount.CanBeEdited(editAllowedHours);
    }

    public async Task<(bool Success, string Message)> RedeemCouponAsync(string couponCode, string merchantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return (false, _localizer["Service_CouponNotFound"]);

        var coupon = await _couponRepository.GetByCodeWithDiscountAsync(couponCode, ct).ConfigureAwait(false);

        if (coupon == null)
            return (false, _localizer["Service_CouponNotFound"]);

        // Verify the coupon belongs to a discount owned by this merchant
        if (coupon.Discount.MerchantId != merchantId)
            return (false, _localizer["Service_NoPermissionToEdit"]);

        if (coupon.Status == CouponStatus.Used)
            return (false, _localizer["Service_CouponAlreadyUsed"]);

        if (coupon.Status != CouponStatus.Purchased)
            return (false, _localizer["Service_CouponNotPurchased"]);

        coupon.Status = CouponStatus.Used;
        coupon.IsUsed = true;
        coupon.UsedAt = DateTime.UtcNow;

        await _couponRepository.UpdateAsync(coupon, ct).ConfigureAwait(false);

        return (true, _localizer["Service_CouponRedeemedSuccessfully"]);
    }
}
