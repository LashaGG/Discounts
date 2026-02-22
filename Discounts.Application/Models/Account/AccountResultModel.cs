namespace Discounts.Application.Models;

/// <summary>
/// Result of a cookie-based authentication operation (register or login).
/// </summary>
public class AccountResultModel
{
    public bool Succeeded { get; set; }
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// The primary role of the authenticated user (e.g., Administrator, Merchant, Customer).
    /// Populated on success to drive post-login redirect logic.
    /// </summary>
    public string? PrimaryRole { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = [];
}
