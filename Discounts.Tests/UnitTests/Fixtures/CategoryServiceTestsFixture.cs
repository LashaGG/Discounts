using Discounts.Application.Interfaces;
using Discounts.Application.Mapping;
using Discounts.Domain.Entities.Business;
using Discounts.Infrastructure.Services;
using Moq;

namespace Discounts.Tests.UnitTests.Fixtures;

public class CategoryServiceTestsFixture
{
    public Mock<ICategoryRepository> CategoryRepoMock { get; }
    public ICategoryService Sut { get; }
    public CancellationToken TestCt { get; }

    static CategoryServiceTestsFixture()
    {
        MappingConfig.Configure();
    }

    public CategoryServiceTestsFixture()
    {
        CategoryRepoMock = new Mock<ICategoryRepository>(MockBehavior.Strict);
        TestCt = new CancellationTokenSource().Token;

        Sut = new CategoryService(CategoryRepoMock.Object);
    }

    public static Category CreateCategory(int id = 1, string name = "Food", bool isActive = true)
        => new()
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
}
