namespace Discounts.Application.DTOs;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalMerchants { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalDiscounts { get; set; }
    public int PendingDiscounts { get; set; }
    public int ActiveDiscounts { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DiscountDto> RecentDiscounts { get; set; } = new();
    public List<UserDto> RecentUsers { get; set; } = new();
}
