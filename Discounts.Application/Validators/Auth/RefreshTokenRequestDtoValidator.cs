using Discounts.Application.DTOs;
using FluentValidation;

namespace Discounts.Application.Validators;

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("ტოკენი აუცილებელია");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh ტოკენი აუცილებელია");
    }
}
