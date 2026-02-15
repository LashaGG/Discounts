namespace Discounts.Application.Models;

public class ReservationResultModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CouponId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? ReservationMinutes { get; set; }
}
