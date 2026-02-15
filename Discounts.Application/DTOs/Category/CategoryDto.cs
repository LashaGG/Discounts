namespace Discounts.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DiscountsCount { get; set; }
}
