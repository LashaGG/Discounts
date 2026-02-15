namespace Discounts.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,        // მოლოდინის რეჟიმში
    Completed = 1,      // დასრულებული
    Cancelled = 2,      // გაუქმებული
    Refunded = 3        // დაბრუნებული
}
