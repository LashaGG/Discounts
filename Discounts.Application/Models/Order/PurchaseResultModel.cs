namespace Discounts.Application.Models;

public class PurchaseResultModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public decimal? Amount { get; set; }
}
