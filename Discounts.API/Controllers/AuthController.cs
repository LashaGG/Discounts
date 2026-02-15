using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<RefreshTokenRequestDto> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<RefreshTokenRequestDto> refreshValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var model = request.Adapt<LoginRequestModel>();
        var result = await _authService.LoginAsync(model, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication Failed",
                Detail = result.Message
            });
        }

        return result.Adapt<AuthResponseDto>();
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var model = request.Adapt<RegisterRequestModel>();
        var result = await _authService.RegisterAsync(model, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration Failed",
                Detail = result.Message
            });
        }

        return CreatedAtAction(nameof(Login), result.Adapt<AuthResponseDto>());
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken ct)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var model = request.Adapt<RefreshTokenRequestModel>();
        var result = await _authService.RefreshTokenAsync(model, ct).ConfigureAwait(false);

        if (!result.Success)
        {   
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Token Refresh Failed",
                Detail = result.Message
            });
        }

        return result.Adapt<AuthResponseDto>();
    }
}
