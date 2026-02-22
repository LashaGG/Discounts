using Discounts.Application;
using Discounts.Application.Commands;
using Discounts.Application.Interfaces;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Mapster;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Discounts.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="CreateDiscountCommand"/> by validating business rules,
/// mapping to the Discount entity, persisting via the repository, and generating coupons.
/// </summary>
public sealed class CreateDiscountCommandHandler : IRequestHandler<CreateDiscountCommand, DiscountModel>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IStringLocalizer<ServiceMessages> _localizer;
    private readonly ISystemSettingsRepository _settingsRepository;

    public CreateDiscountCommandHandler(
        IDiscountRepository discountRepository,
        ICouponRepository couponRepository,
        IStringLocalizer<ServiceMessages> localizer,
        ISystemSettingsRepository settingsRepository)
    {
        _discountRepository = discountRepository;
        _couponRepository = couponRepository;
        _localizer = localizer;
        _settingsRepository = settingsRepository;
    }

    public async Task<DiscountModel> Handle(CreateDiscountCommand command, CancellationToken ct)
    {
        if (command.DiscountedPrice >= command.OriginalPrice)
            throw new InvalidOperationException(_localizer["Service_DiscountedPriceMustBeLess"]);

        var validFrom = command.ValidFrom.Kind != DateTimeKind.Utc
            ? command.ValidFrom.ToUniversalTime()
            : command.ValidFrom;

        var validTo = command.ValidTo.Kind != DateTimeKind.Utc
            ? command.ValidTo.ToUniversalTime()
            : command.ValidTo;

        if (validFrom >= validTo)
            throw new InvalidOperationException(_localizer["Service_EndDateMustBeAfterStart"]);

        if (validFrom < DateTime.UtcNow.AddHours(-1))
            throw new InvalidOperationException(_localizer["Service_StartDateCannotBeInPast"]);

        var discount = command.Adapt<Discount>();
        discount.ValidFrom = validFrom;
        discount.ValidTo = validTo;

        var created = await _discountRepository.CreateAsync(discount, ct).ConfigureAwait(false);

        var coupons = new List<Coupon>();
        for (var i = 0; i < command.TotalCoupons; i++)
        {
            var code = await _couponRepository.GenerateUniqueCouponCodeAsync(ct).ConfigureAwait(false);
            coupons.Add(new Coupon
            {
                Code = code,
                DiscountId = created.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _couponRepository.CreateBulkAsync(coupons, ct).ConfigureAwait(false);

        return created.Adapt<DiscountModel>();
    }
}
