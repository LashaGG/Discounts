namespace Discounts.Application.DTOs;

public class SalesHistoryDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> CouponCodes { get; set; } = new();
}
