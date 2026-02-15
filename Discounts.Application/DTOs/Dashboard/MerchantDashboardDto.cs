namespace Discounts.Application.DTOs;

public class MerchantDashboardDto
{
    public int TotalDiscounts { get; set; }
    public int ActiveDiscounts { get; set; }
    public int PendingDiscounts { get; set; }
    public int ExpiredDiscounts { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DiscountDto> RecentDiscounts { get; set; } = new();
    public List<SalesStatDto> SalesStats { get; set; } = new();
}
