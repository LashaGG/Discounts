namespace Discounts.Application.DTOs;

public class UpdateDiscountDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
}
