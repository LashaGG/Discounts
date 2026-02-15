namespace Discounts.Domain.Constants;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Merchant = "Merchant";
    public const string Customer = "Customer";

    public static IReadOnlyList<string> All { get; } =
    [
        Administrator,
        Merchant,
        Customer
    ];
}
