using Microsoft.AspNetCore.Identity;

namespace Discounts.Domain.Entities.Core;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
}
