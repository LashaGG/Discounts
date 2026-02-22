using Discounts.Application.Interfaces;
using Discounts.Domain.Entities.Business;
using Discounts.Tests.UnitTests.Fixtures;
using FluentAssertions;
using Moq;

namespace Discounts.Tests.UnitTests.ApplicationLayer;

public class CategoryServiceTests : IClassFixture<CategoryServiceTestsFixture>
{
    private readonly ICategoryService _sut;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly CancellationToken _testCt;

    public CategoryServiceTests(CategoryServiceTestsFixture fixture)
    {
        _sut = fixture.Sut;
        _categoryRepoMock = fixture.CategoryRepoMock;
        _testCt = fixture.TestCt;

        _categoryRepoMock.Invocations.Clear();
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_WhenCategoriesExist_ReturnsMappedModels()
    {
        var categories = new List<Category>
        {
            CategoryServiceTestsFixture.CreateCategory(1, "Food"),
            CategoryServiceTestsFixture.CreateCategory(2, "Travel")
        };

        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(_testCt))
            .ReturnsAsync(categories);

        var result = await _sut.GetActiveCategoriesAsync(_testCt).ConfigureAwait(true);

        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_WhenNone_ReturnsEmptyCollection()
    {
        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(_testCt))
            .ReturnsAsync(new List<Category>());

        var result = await _sut.GetActiveCategoriesAsync(_testCt).ConfigureAwait(true);

        result.Should().BeEmpty();
    }
}
