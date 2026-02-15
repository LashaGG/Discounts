namespace Discounts.Application.Models;

public class CustomerDashboardModel
{
    public int TotalCoupons { get; set; }
    public int ActiveCoupons { get; set; }
    public int UsedCoupons { get; set; }
    public int ExpiredCoupons { get; set; }
    public decimal TotalSavings { get; set; }
    public List<CouponModel> RecentPurchases { get; set; } = new();
}
