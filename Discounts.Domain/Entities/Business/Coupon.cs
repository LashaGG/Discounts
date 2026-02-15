using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;

namespace Discounts.Domain.Entities.Business;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // Unique coupon code

    // Discount relationship
    public int DiscountId { get; set; }
    public Discount Discount { get; set; } = null!;

    // Customer relationship
    public string? CustomerId { get; set; }
    public ApplicationUser? Customer { get; set; }

    // Status tracking
    public CouponStatus Status { get; set; } = CouponStatus.Available;
    public bool IsUsed { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Order relationship
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}
