namespace Discounts.Application.Models;

public class SalesStatModel
{
    public string DiscountTitle { get; set; } = string.Empty;
    public int CouponsSold { get; set; }
    public decimal Revenue { get; set; }
}
