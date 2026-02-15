using Discounts.Domain.Constants;

namespace Discounts.Web.Controllers;

public class RegisterViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Customer;
    
    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
}
