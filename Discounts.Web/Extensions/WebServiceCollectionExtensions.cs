using Discounts.Domain.Constants;

namespace Discounts.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MVC controllers with localization support.
    /// </summary>
    public static IServiceCollection AddWebPresentation(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();

        return services;
    }

    /// <summary>
    /// Registers role-based authorization policies for the Web layer.
    /// </summary>
    public static IServiceCollection AddWebAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole(Roles.Administrator));

            options.AddPolicy("RequireMerchant", policy =>
                policy.RequireRole(Roles.Merchant));

            options.AddPolicy("RequireCustomer", policy =>
                policy.RequireRole(Roles.Customer));

            options.AddPolicy("MerchantOrAdmin", policy =>
                policy.RequireRole(Roles.Administrator, Roles.Merchant));
        });

        return services;
    }
}
