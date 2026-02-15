namespace Discounts.Application.Models;

public class CreateCategoryModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
}
