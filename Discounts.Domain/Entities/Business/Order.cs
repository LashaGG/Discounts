using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;

namespace Discounts.Domain.Entities.Business;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;  // Unique order number
    
    // Customer
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser Customer { get; set; } = null!;
    
    // Discount
    public int DiscountId { get; set; }
    public Discount Discount { get; set; } = null!;
    
    // Pricing
    public decimal TotalAmount { get; set; }
    public int Quantity { get; set; }  // Number of coupons purchased
    
    // Status
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    // Dates
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Payment info (simplified for now)
    public string? PaymentMethod { get; set; }
    public string? PaymentTransactionId { get; set; }
    
    // Navigation
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}
