namespace Discounts.Application.Models;

public class RefreshTokenRequestModel
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
