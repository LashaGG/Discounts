namespace Discounts.Application.DTOs;

public class CreateDiscountDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int TotalCoupons { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
}
