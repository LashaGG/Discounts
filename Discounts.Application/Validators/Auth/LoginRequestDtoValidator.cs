using Discounts.Application.DTOs;
using FluentValidation;

namespace Discounts.Application.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ელ-ფოსტა აუცილებელია")
            .EmailAddress().WithMessage("ელ-ფოსტის ფორმატი არასწორია");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("პაროლი აუცილებელია");
    }
}
