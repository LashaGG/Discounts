using Discounts.Domain.Enums;

namespace Discounts.Application.Models;

public class CouponModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DiscountId { get; set; }
    public string DiscountTitle { get; set; } = string.Empty;
    public string? DiscountImageUrl { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public CouponStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}
