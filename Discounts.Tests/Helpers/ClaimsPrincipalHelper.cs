using System.Security.Claims;

namespace Discounts.Tests.Helpers;

internal static class ClaimsPrincipalHelper
{
    internal static ClaimsPrincipal CreateAuthenticatedUser(string userId, string role = "Merchant")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    internal static ClaimsPrincipal CreateAnonymousUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }
}
