using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Controllers;

/// <summary>
/// Handles culture/language switching via cookie.
/// </summary>
public class CultureController : Controller
{
    [HttpPost]
    public IActionResult SetLanguage(string culture, string? returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }
}
