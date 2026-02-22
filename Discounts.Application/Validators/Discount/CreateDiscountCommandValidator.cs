using Discounts.Application.Commands;
using Discounts.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Discounts.Application.Validators;

public class CreateDiscountCommandValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(localizer["Validation_TitleRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["Validation_TitleMaxLength"].Value);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(localizer["Validation_DescriptionRequired"].Value)
            .MaximumLength(2000).WithMessage(localizer["Validation_DescriptionMaxLength2000"].Value);

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(0).WithMessage(localizer["Validation_OriginalPriceGreaterThanZero"].Value)
            .LessThanOrEqualTo(1_000_000).WithMessage(localizer["Validation_PriceMaxLimit"].Value);

        RuleFor(x => x.DiscountedPrice)
            .GreaterThan(0).WithMessage(localizer["Validation_DiscountedPriceGreaterThanZero"].Value)
            .LessThan(x => x.OriginalPrice).WithMessage(localizer["Validation_DiscountedPriceLessThanOriginal"].Value);

        RuleFor(x => x.TotalCoupons)
            .InclusiveBetween(1, 100_000).WithMessage(localizer["Validation_CouponCountRange"].Value);

        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage(localizer["Validation_StartDateRequired"].Value)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage(localizer["Validation_StartDateNotInPast"].Value);

        RuleFor(x => x.ValidTo)
            .NotEmpty().WithMessage(localizer["Validation_EndDateRequired"].Value)
            .GreaterThan(x => x.ValidFrom).WithMessage(localizer["Validation_EndDateAfterStartDate"].Value);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage(localizer["Validation_CategoryRequired"].Value);

        RuleFor(x => x.MerchantId)
            .NotEmpty();

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage(localizer["Validation_UrlMaxLength"].Value)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage(localizer["Validation_InvalidUrlFormat"].Value);
    }
}
