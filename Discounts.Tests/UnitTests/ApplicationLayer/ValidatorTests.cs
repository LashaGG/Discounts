using Discounts.Application.DTOs;
using Discounts.Application.Resources;
using Discounts.Application.Validators;
using Discounts.Domain.Constants;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

/// <summary>
/// Validates that FluentValidation rules defined in the Application layer
/// enforce required fields, length constraints, and business invariants.
/// </summary>
public class ValidatorTests
{
    // Localized validators share a common localizer mock
    private static readonly Mock<IStringLocalizer<ValidationMessages>> ValidLocalizerMock = CreateLocalizerMock();

    private static Mock<IStringLocalizer<ValidationMessages>> CreateLocalizerMock()
    {
        var mock = new Mock<IStringLocalizer<ValidationMessages>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return mock;
    }

    public class CreateDiscountDtoValidatorTests : ValidatorTests
    {
        private readonly CreateDiscountDtoValidator _validator = new(ValidLocalizerMock.Object);

        private static CreateDiscountDto ValidDto() => new()
        {
            Title = "Summer Sale",
            Description = "Great discount",
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            TotalCoupons = 50,
            ValidFrom = DateTime.UtcNow.Date,
            ValidTo = DateTime.UtcNow.Date.AddDays(30),
            CategoryId = 1
        };

        [Fact]
        public void WhenAllFieldsValid_ShouldPassValidation()
        {
            var result = _validator.TestValidate(ValidDto());

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WhenTitleEmptyOrNull_ShouldFail(string? title)
        {
            var dto = ValidDto();
            dto.Title = title!;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void WhenTitleExceedsMaxLength_ShouldFail()
        {
            var dto = ValidDto();
            dto.Title = new string('A', 201);

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WhenOriginalPriceNotPositive_ShouldFail(decimal price)
        {
            var dto = ValidDto();
            dto.OriginalPrice = price;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.OriginalPrice);
        }

        [Fact]
        public void WhenOriginalPriceExceedsLimit_ShouldFail()
        {
            var dto = ValidDto();
            dto.OriginalPrice = 1_000_001m;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.OriginalPrice);
        }

        [Fact]
        public void WhenDiscountedPriceNotLessThanOriginal_ShouldFail()
        {
            var dto = ValidDto();
            dto.DiscountedPrice = 100m;
            dto.OriginalPrice = 100m;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.DiscountedPrice);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100_001)]
        public void WhenTotalCouponsOutOfRange_ShouldFail(int count)
        {
            var dto = ValidDto();
            dto.TotalCoupons = count;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.TotalCoupons);
        }

        [Fact]
        public void WhenEndDateNotAfterStartDate_ShouldFail()
        {
            var dto = ValidDto();
            dto.ValidTo = dto.ValidFrom.AddDays(-1);

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ValidTo);
        }

        [Fact]
        public void WhenCategoryIdZero_ShouldFail()
        {
            var dto = ValidDto();
            dto.CategoryId = 0;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        }

        [Fact]
        public void WhenImageUrlInvalid_ShouldFail()
        {
            var dto = ValidDto();
            dto.ImageUrl = "not-a-url";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
        }

        [Fact]
        public void WhenImageUrlNull_ShouldPass()
        {
            var dto = ValidDto();
            dto.ImageUrl = null;

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
        }
    }


    public class UpdateDiscountDtoValidatorTests : ValidatorTests
    {
        private readonly UpdateDiscountDtoValidator _validator = new(ValidLocalizerMock.Object);

        private static UpdateDiscountDto ValidDto() => new()
        {
            Id = 1,
            Title = "Updated Sale",
            Description = "Updated description",
            OriginalPrice = 100m,
            DiscountedPrice = 60m,
            ValidFrom = DateTime.UtcNow.Date,
            ValidTo = DateTime.UtcNow.Date.AddDays(30),
            CategoryId = 1
        };

        [Fact]
        public void WhenAllFieldsValid_ShouldPassValidation()
        {
            var result = _validator.TestValidate(ValidDto());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void WhenIdIsZero_ShouldFail()
        {
            var dto = ValidDto();
            dto.Id = 0;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Fact]
        public void WhenDiscountedPriceNotLessThanOriginal_ShouldFail()
        {
            var dto = ValidDto();
            dto.DiscountedPrice = dto.OriginalPrice;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.DiscountedPrice);
        }
    }

    public class CreateCategoryDtoValidatorTests : ValidatorTests
    {
        private readonly CreateCategoryDtoValidator _validator = new(ValidLocalizerMock.Object);

        [Fact]
        public void WhenNameProvided_ShouldPassValidation()
        {
            var dto = new CreateCategoryDto { Name = "Food" };

            var result = _validator.TestValidate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WhenNameEmpty_ShouldFail(string? name)
        {
            var dto = new CreateCategoryDto { Name = name! };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void WhenNameExceedsMaxLength_ShouldFail()
        {
            var dto = new CreateCategoryDto { Name = new string('A', 101) };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void WhenDescriptionExceedsMaxLength_ShouldFail()
        {
            var dto = new CreateCategoryDto { Name = "Food", Description = new string('A', 501) };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Description);
        }
    }

    public class UpdateCategoryDtoValidatorTests : ValidatorTests
    {
        private readonly UpdateCategoryDtoValidator _validator = new(ValidLocalizerMock.Object);

        [Fact]
        public void WhenIdIsZero_ShouldFail()
        {
            var dto = new UpdateCategoryDto { Id = 0, Name = "Food" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Fact]
        public void WhenValid_ShouldPass()
        {
            var dto = new UpdateCategoryDto { Id = 1, Name = "Food" };

            var result = _validator.TestValidate(dto);

            result.IsValid.Should().BeTrue();
        }
    }


    public class LoginRequestDtoValidatorTests
    {
        private readonly LoginRequestDtoValidator _validator = new();

        [Fact]
        public void WhenValid_ShouldPass()
        {
            var dto = new LoginRequestDto { Email = "test@test.com", Password = "password" };

            var result = _validator.TestValidate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void WhenEmailEmpty_ShouldFail()
        {
            var dto = new LoginRequestDto { Email = "", Password = "password" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void WhenEmailInvalidFormat_ShouldFail()
        {
            var dto = new LoginRequestDto { Email = "not-an-email", Password = "password" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void WhenPasswordEmpty_ShouldFail()
        {
            var dto = new LoginRequestDto { Email = "test@test.com", Password = "" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }

    public class RegisterRequestDtoValidatorTests
    {
        private readonly RegisterRequestDtoValidator _validator = new();

        private static RegisterRequestDto ValidDto() => new()
        {
            Email = "merchant@test.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "John",
            LastName = "Doe",
            Role = Roles.Customer
        };

        [Fact]
        public void WhenAllFieldsValid_ShouldPass()
        {
            var result = _validator.TestValidate(ValidDto());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void WhenPasswordTooShort_ShouldFail()
        {
            var dto = ValidDto();
            dto.Password = "Ab1!";
            dto.ConfirmPassword = "Ab1!";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void WhenPasswordMissingUppercase_ShouldFail()
        {
            var dto = ValidDto();
            dto.Password = "password1!";
            dto.ConfirmPassword = "password1!";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void WhenPasswordMissingDigit_ShouldFail()
        {
            var dto = ValidDto();
            dto.Password = "Password!";
            dto.ConfirmPassword = "Password!";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void WhenPasswordMissingSpecialChar_ShouldFail()
        {
            var dto = ValidDto();
            dto.Password = "Password1";
            dto.ConfirmPassword = "Password1";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void WhenConfirmPasswordDoesNotMatch_ShouldFail()
        {
            var dto = ValidDto();
            dto.ConfirmPassword = "DifferentPassword1!";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
        }

        [Fact]
        public void WhenRoleInvalid_ShouldFail()
        {
            var dto = ValidDto();
            dto.Role = "InvalidRole";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void WhenMerchantRoleAndCompanyNameMissing_ShouldFail()
        {
            var dto = ValidDto();
            dto.Role = Roles.Merchant;
            dto.CompanyName = null;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.CompanyName);
        }

        [Fact]
        public void WhenCustomerRoleAndNoCompanyName_ShouldPass()
        {
            var dto = ValidDto();
            dto.Role = Roles.Customer;
            dto.CompanyName = null;

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.CompanyName);
        }
    }
    public class RefreshTokenRequestDtoValidatorTests
    {
        private readonly RefreshTokenRequestDtoValidator _validator = new();

        [Fact]
        public void WhenValid_ShouldPass()
        {
            var dto = new RefreshTokenRequestDto { Token = "jwt-token", RefreshToken = "refresh-token" };

            var result = _validator.TestValidate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void WhenTokenEmpty_ShouldFail()
        {
            var dto = new RefreshTokenRequestDto { Token = "", RefreshToken = "refresh-token" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Token);
        }

        [Fact]
        public void WhenRefreshTokenEmpty_ShouldFail()
        {
            var dto = new RefreshTokenRequestDto { Token = "jwt-token", RefreshToken = "" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
        }
    }
}
