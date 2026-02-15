namespace Discounts.Application.Models;

public class MerchantDashboardModel
{
    public int TotalDiscounts { get; set; }
    public int ActiveDiscounts { get; set; }
    public int PendingDiscounts { get; set; }
    public int ExpiredDiscounts { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DiscountModel> RecentDiscounts { get; set; } = new();
    public List<SalesStatModel> SalesStats { get; set; } = new();
}
