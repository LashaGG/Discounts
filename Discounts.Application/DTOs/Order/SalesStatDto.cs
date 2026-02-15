namespace Discounts.Application.DTOs;

public class SalesStatDto
{
    public string DiscountTitle { get; set; } = string.Empty;
    public int CouponsSold { get; set; }
    public decimal Revenue { get; set; }
}
