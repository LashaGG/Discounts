namespace Discounts.Domain.Entities.Configuration;

public class SystemSettings
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }
}
