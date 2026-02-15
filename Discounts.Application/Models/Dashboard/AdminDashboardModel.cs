namespace Discounts.Application.Models;

public class AdminDashboardModel
{
    public int TotalUsers { get; set; }
    public int TotalMerchants { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalDiscounts { get; set; }
    public int PendingDiscounts { get; set; }
    public int ActiveDiscounts { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DiscountModel> RecentDiscounts { get; set; } = new();
    public List<UserModel> RecentUsers { get; set; } = new();
}
