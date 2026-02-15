using Discounts.Application.DTOs;
using Discounts.Application.Models;
using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Core;
using Mapster;

namespace Discounts.Application.Mapping;

public static class MappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Discount, DiscountModel>.NewConfig()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.MerchantName, src => src.Merchant != null
                ? (src.Merchant.CompanyName ?? $"{src.Merchant.FirstName} {src.Merchant.LastName}".Trim())
                : string.Empty);

        TypeAdapterConfig<Coupon, CouponModel>.NewConfig()
            .Map(dest => dest.DiscountTitle, src => src.Discount.Title)
            .Map(dest => dest.DiscountImageUrl, src => src.Discount.ImageUrl)
            .Map(dest => dest.MerchantName, src => src.Discount.Merchant != null
                ? (src.Discount.Merchant.CompanyName ?? $"{src.Discount.Merchant.FirstName} {src.Discount.Merchant.LastName}".Trim())
                : string.Empty)
            .Map(dest => dest.CategoryName, src => src.Discount.Category != null ? src.Discount.Category.Name : string.Empty)
            .Map(dest => dest.OriginalPrice, src => src.Discount.OriginalPrice)
            .Map(dest => dest.DiscountedPrice, src => src.Discount.DiscountedPrice)
            .Map(dest => dest.DiscountPercentage, src => (int)src.Discount.DiscountPercentage)
            .Map(dest => dest.ValidFrom, src => src.Discount.ValidFrom)
            .Map(dest => dest.ValidTo, src => src.Discount.ValidTo);

        TypeAdapterConfig<ApplicationUser, UserModel>.NewConfig()
            .Map(dest => dest.Roles, _ => new List<string>());

        TypeAdapterConfig<Category, CategoryModel>.NewConfig()
            .Map(dest => dest.DiscountsCount, _ => 0);

        TypeAdapterConfig<CreateDiscountModel, Discount>.NewConfig()
            .Map(dest => dest.AvailableCoupons, src => src.TotalCoupons)
            .Map(dest => dest.Status, _ => Domain.Enums.DiscountStatus.Pending)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.MerchantId)
            .Ignore(dest => dest.Category!)
            .Ignore(dest => dest.Merchant!);

        TypeAdapterConfig<CreateCategoryModel, Category>.NewConfig()
            .Map(dest => dest.IsActive, _ => true)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id);

        TypeAdapterConfig<DiscountModel, DiscountDto>.NewConfig();
        TypeAdapterConfig<DiscountDto, DiscountModel>.NewConfig();

        TypeAdapterConfig<CreateDiscountDto, CreateDiscountModel>.NewConfig();
        TypeAdapterConfig<UpdateDiscountDto, UpdateDiscountModel>.NewConfig();

        TypeAdapterConfig<CouponModel, CouponDto>.NewConfig();
        TypeAdapterConfig<UserModel, UserDto>.NewConfig();
        TypeAdapterConfig<CategoryModel, CategoryDto>.NewConfig();

        TypeAdapterConfig<CreateCategoryDto, CreateCategoryModel>.NewConfig();
        TypeAdapterConfig<UpdateCategoryDto, UpdateCategoryModel>.NewConfig();

        TypeAdapterConfig<MerchantDashboardModel, MerchantDashboardDto>.NewConfig();
        TypeAdapterConfig<SalesStatModel, SalesStatDto>.NewConfig();
        TypeAdapterConfig<SalesHistoryModel, SalesHistoryDto>.NewConfig();
        TypeAdapterConfig<AdminDashboardModel, AdminDashboardDto>.NewConfig();

        TypeAdapterConfig<ReservationResultModel, ReservationResultDto>.NewConfig();
        TypeAdapterConfig<PurchaseResultModel, PurchaseResultDto>.NewConfig();

        TypeAdapterConfig<DiscountFilterDto, DiscountFilterModel>.NewConfig();

        TypeAdapterConfig<LoginRequestDto, LoginRequestModel>.NewConfig();
        TypeAdapterConfig<RegisterRequestDto, RegisterRequestModel>.NewConfig();
        TypeAdapterConfig<RefreshTokenRequestDto, RefreshTokenRequestModel>.NewConfig();
        TypeAdapterConfig<AuthResponseModel, AuthResponseDto>.NewConfig();
    }
}
