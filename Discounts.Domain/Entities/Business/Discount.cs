using System.ComponentModel.DataAnnotations;
using Discounts.Domain.Entities.Core;
using Discounts.Domain.Enums;

namespace Discounts.Domain.Entities.Business;

public class Discount
{
    public int Id { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
    
    // Basic Info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    
    // Pricing
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal DiscountPercentage => OriginalPrice > 0 
        ? Math.Round((OriginalPrice - DiscountedPrice) / OriginalPrice * 100, 2) 
        : 0;
    
    // Availability
    public int TotalCoupons { get; set; }
    public int AvailableCoupons { get; set; }
    public int SoldCoupons => TotalCoupons - AvailableCoupons;
    
    // Dates
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    
    // Status
    public DiscountStatus Status { get; set; } = DiscountStatus.Pending;
    
    // Relationships
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public string MerchantId { get; set; } = string.Empty;
    public ApplicationUser Merchant { get; set; } = null!;
    
    // Admin approval
    public string? ApprovedByAdminId { get; set; }
    public ApplicationUser? ApprovedByAdmin { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Navigation properties
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    
    // Business Logic
    public bool IsActive => Status == DiscountStatus.Active && 
                           DateTime.UtcNow >= ValidFrom && 
                           DateTime.UtcNow <= ValidTo &&
                           AvailableCoupons > 0;
    
    public bool IsExpired => DateTime.UtcNow > ValidTo;
    
    public bool CanBeEdited(int allowedEditHours = 24)
    {
        if (Status != DiscountStatus.Pending &&
            Status != DiscountStatus.Rejected &&
            Status != DiscountStatus.Approved)
            return false;

        var createdTimeLimit = CreatedAt.AddHours(allowedEditHours);
        return DateTime.UtcNow <= createdTimeLimit;
    }
}
