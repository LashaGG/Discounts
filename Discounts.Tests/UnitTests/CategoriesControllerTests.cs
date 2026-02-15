using Discounts.Application.Models;
using Discounts.Tests.Controllers.Fixtures;
using FluentAssertions;
using Moq;

namespace Discounts.Tests.Controllers;

public sealed class CategoriesControllerTests
{
    private readonly CategoriesControllerFixture _f;

    public CategoriesControllerTests()
    {
        _f = new CategoriesControllerFixture();
    }

    [Fact]
    public async Task GetActiveCategories_WhenCalled_ShouldReturnMappedCategories()
    {
        var models = new List<CategoryModel>
        {
            new() { Id = 1, Name = "Food", Description = "Food items", IconClass = "fa-food", IsActive = true, CreatedAt = new DateTime(2024, 1, 1), DiscountsCount = 10 },
            new() { Id = 2, Name = "Tech", Description = "Technology", IconClass = "fa-tech", IsActive = true, CreatedAt = new DateTime(2024, 2, 1), DiscountsCount = 5 }
        };

        _f.CategoryService
            .Setup(s => s.GetActiveCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        var result = await _f.Sut.GetActiveCategories(CancellationToken.None).ConfigureAwait(true);

        var list = result.Value?.ToList();
        list.Should().NotBeNull();
        list.Should().HaveCount(2);

        list![0].Id.Should().Be(1);
        list[0].Name.Should().Be("Food");
        list[0].Description.Should().Be("Food items");
        list[0].IconClass.Should().Be("fa-food");
        list[0].IsActive.Should().BeTrue();
        list[0].DiscountsCount.Should().Be(10);

        list[1].Id.Should().Be(2);
        list[1].Name.Should().Be("Tech");
        list[1].Description.Should().Be("Technology");
        list[1].IconClass.Should().Be("fa-tech");
        list[1].IsActive.Should().BeTrue();
        list[1].DiscountsCount.Should().Be(5);
    }

    [Fact]
    public async Task GetActiveCategories_WhenEmpty_ShouldReturnEmptyList()
    {
        _f.CategoryService
            .Setup(s => s.GetActiveCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryModel>());

        var result = await _f.Sut.GetActiveCategories(CancellationToken.None).ConfigureAwait(true);

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveCategories_WhenServiceThrows_ShouldPropagateException()
    {
        _f.CategoryService
            .Setup(s => s.GetActiveCategoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var act = () => _f.Sut.GetActiveCategories(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error").ConfigureAwait(true);
    }
}
