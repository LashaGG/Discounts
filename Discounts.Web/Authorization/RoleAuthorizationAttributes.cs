using Discounts.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Discounts.Web.Authorization;

/// <summary>
/// Authorization attribute for Administrator role
/// </summary>
public class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Roles = Domain.Constants.Roles.Administrator;
    }
}

/// <summary>
/// Authorization attribute for Merchant role
/// </summary>
public class AuthorizeMerchantAttribute : AuthorizeAttribute
{
    public AuthorizeMerchantAttribute()
    {
        Roles = Domain.Constants.Roles.Merchant;
    }
}

/// <summary>
/// Authorization attribute for Customer role
/// </summary>
public class AuthorizeCustomerAttribute : AuthorizeAttribute
{
    public AuthorizeCustomerAttribute()
    {
        Roles = Domain.Constants.Roles.Customer;
    }
}

/// <summary>
/// Authorization attribute for Merchant or Administrator roles
/// </summary>
public class AuthorizeMerchantOrAdminAttribute : AuthorizeAttribute
{
    public AuthorizeMerchantOrAdminAttribute()
    {
        Roles = $"{Domain.Constants.Roles.Merchant},{Domain.Constants.Roles.Administrator}";
    }
}
