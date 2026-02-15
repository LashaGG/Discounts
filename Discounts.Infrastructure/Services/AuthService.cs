// Copyright (C) TBC Bank. All Rights Reserved.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Constants;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Discounts.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseModel> LoginAsync(LoginRequestModel request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
        {
            return new AuthResponseModel { Success = false, Message = "არასწორი ელ-ფოსტა ან პაროლი" };
        }

        if (!user.IsActive)
        {
            return new AuthResponseModel { Success = false, Message = "ანგარიში დეაქტივირებულია" };
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = GenerateJwtToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(
            user, "Discounts.API", "RefreshToken", refreshToken).ConfigureAwait(false);

        return new AuthResponseModel
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            UserId = user.Id,
            Email = user.Email!,
            Roles = roles.ToList()
        };
    }

    public async Task<AuthResponseModel> RegisterAsync(RegisterRequestModel request, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser != null)
        {
            return new AuthResponseModel { Success = false, Message = "ელ-ფოსტა უკვე რეგისტრირებულია" };
        }

        if (request.Role != Roles.Customer && request.Role != Roles.Merchant)
        {
            return new AuthResponseModel { Success = false, Message = "დაუშვებელი როლი" };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResponseModel { Success = false, Message = errors };
        }

        await _userManager.AddToRoleAsync(user, request.Role).ConfigureAwait(false);

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = GenerateJwtToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(
            user, "Discounts.API", "RefreshToken", refreshToken).ConfigureAwait(false);

        return new AuthResponseModel
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            UserId = user.Id,
            Email = user.Email,
            Roles = roles.ToList(),
            Message = "რეგისტრაცია წარმატებით დასრულდა"
        };
    }

    public async Task<AuthResponseModel> RefreshTokenAsync(RefreshTokenRequestModel request, CancellationToken ct = default)
    {
        var principal = GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return new AuthResponseModel { Success = false, Message = "არასწორი ტოკენი" };
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthResponseModel { Success = false, Message = "არასწორი ტოკენი" };
        }

        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || !user.IsActive)
        {
            return new AuthResponseModel { Success = false, Message = "მომხმარებელი ვერ მოიძებნა" };
        }

        var storedRefreshToken = await _userManager.GetAuthenticationTokenAsync(
            user, "Discounts.API", "RefreshToken").ConfigureAwait(false);

        if (storedRefreshToken != request.RefreshToken)
        {
            return new AuthResponseModel { Success = false, Message = "არასწორი refresh ტოკენი" };
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var newToken = GenerateJwtToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(
            user, "Discounts.API", "RefreshToken", newRefreshToken).ConfigureAwait(false);

        return new AuthResponseModel
        {
            Success = true,
            Token = newToken,
            RefreshToken = newRefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            UserId = user.Id,
            Email = user.Email!,
            Roles = roles.ToList()
        };
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
            ValidateLifetime = false // Allow expired tokens for refresh
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }

    private int GetTokenExpirationMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpirationInMinutes"], out var minutes)
            ? minutes
            : 60;
    }
}
