using Discounts.Application.DTOs;
using Discounts.Domain.Constants;
using FluentValidation;

namespace Discounts.Application.Validators;

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ელ-ფოსტა აუცილებელია")
            .EmailAddress().WithMessage("ელ-ფოსტის ფორმატი არასწორია");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("პაროლი აუცილებელია")
            .MinimumLength(8).WithMessage("პაროლი მინიმუმ 8 სიმბოლო უნდა იყოს")
            .Matches("[A-Z]").WithMessage("პაროლი უნდა შეიცავდეს მინიმუმ ერთ დიდ ასოს")
            .Matches("[a-z]").WithMessage("პაროლი უნდა შეიცავდეს მინიმუმ ერთ პატარა ასოს")
            .Matches("[0-9]").WithMessage("პაროლი უნდა შეიცავდეს მინიმუმ ერთ ციფრს")
            .Matches("[^a-zA-Z0-9]").WithMessage("პაროლი უნდა შეიცავდეს მინიმუმ ერთ სპეციალურ სიმბოლოს");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("პაროლები არ ემთხვევა");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("სახელი აუცილებელია")
            .MaximumLength(100).WithMessage("სახელი არ უნდა აღემატებოდეს 100 სიმბოლოს");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("გვარი აუცილებელია")
            .MaximumLength(100).WithMessage("გვარი არ უნდა აღემატებოდეს 100 სიმბოლოს");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("როლი აუცილებელია")
            .Must(r => r == Roles.Merchant || r == Roles.Customer)
            .WithMessage("როლი უნდა იყოს Merchant ან Customer");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("კომპანიის სახელი აუცილებელია მერჩანტისთვის")
            .When(x => x.Role == Roles.Merchant);
    }
}
