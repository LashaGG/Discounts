using Discounts.Application.DTOs;
using Discounts.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Discounts.Application.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation_NameRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["Validation_NameMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(localizer["Validation_DescriptionMaxLength500"].Value);

        RuleFor(x => x.IconClass)
            .MaximumLength(50).WithMessage(localizer["Validation_IconClassMaxLength"].Value);
    }
}
