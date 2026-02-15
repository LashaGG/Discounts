using Discounts.Application.Models;

namespace Discounts.Application.Interfaces;

/// <summary>
/// Handles JWT-based authentication operations for the API layer.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseModel> LoginAsync(LoginRequestModel request, CancellationToken ct = default);
    Task<AuthResponseModel> RegisterAsync(RegisterRequestModel request, CancellationToken ct = default);
    Task<AuthResponseModel> RefreshTokenAsync(RefreshTokenRequestModel request, CancellationToken ct = default);
}
