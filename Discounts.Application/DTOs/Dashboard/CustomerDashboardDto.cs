namespace Discounts.Application.DTOs;

public class CustomerDashboardDto
{
    public int TotalCoupons { get; set; }
    public int ActiveCoupons { get; set; }
    public int UsedCoupons { get; set; }
    public int ExpiredCoupons { get; set; }
    public decimal TotalSavings { get; set; }
    public List<CouponDto> RecentPurchases { get; set; } = new();
}
