using Discounts.Domain.Enums;

namespace Discounts.Application.DTOs;

public class CouponDto
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
    public bool IsExpired => DateTime.UtcNow > ValidTo;
    public bool IsActive => Status == CouponStatus.Purchased && !IsExpired;
    public string StatusText => GetStatusText();
    public string StatusColor => GetStatusColor();

    private string GetStatusText()
    {
        if (IsExpired && Status == CouponStatus.Purchased)
            return "ვადაგასული";

        return Status switch
        {
            CouponStatus.Available => "ხელმისაწვდომი",
            CouponStatus.Reserved => "დაჯავშნილი",
            CouponStatus.Purchased => "შეძენილი",
            CouponStatus.Used => "გამოყენებული",
            CouponStatus.Expired => "ვადაგასული",
            _ => Status.ToString()
        };
    }

    private string GetStatusColor()
    {
        if (IsExpired && Status == CouponStatus.Purchased)
            return "secondary";

        return Status switch
        {
            CouponStatus.Available => "success",
            CouponStatus.Reserved => "warning",
            CouponStatus.Purchased => "primary",
            CouponStatus.Used => "info",
            CouponStatus.Expired => "secondary",
            _ => "secondary"
        };
    }
}
