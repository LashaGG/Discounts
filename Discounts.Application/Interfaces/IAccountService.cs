using Discounts.Application.Models;

namespace Discounts.Application.Interfaces;

/// <summary>
/// Handles cookie-based authentication operations for the MVC Web layer.
/// Encapsulates Identity's UserManager and SignInManager behind a clean boundary.
/// </summary>
public interface IAccountService
{
    Task<AccountResultModel> RegisterAsync(RegisterAccountModel model);
    Task<AccountResultModel> LoginAsync(LoginAccountModel model);
    Task LogoutAsync();
}
