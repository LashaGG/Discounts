using Discounts.Application.Models;
using MediatR;

namespace Discounts.Application.Commands;

/// <summary>
/// Command to create a new discount offer. Acts as the business model for the CQRS create flow.
/// </summary>
public record CreateDiscountCommand(
    string Title,
    string Description,
    string? ImageUrl,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int TotalCoupons,
    DateTime ValidFrom,
    DateTime ValidTo,
    int CategoryId,
    string MerchantId) : IRequest<DiscountModel>;
