using Discounts.API.Controllers;
using Discounts.Application.Interfaces;
using Discounts.Tests.Helpers;
using Moq;

namespace Discounts.Tests.Controllers.Fixtures;

internal sealed class CategoriesControllerFixture
{
    public Mock<ICategoryService> CategoryService { get; }
    public CategoriesController Sut { get; }

    public CategoriesControllerFixture()
    {
        MapsterSetup.EnsureConfigured();

        CategoryService = new Mock<ICategoryService>(MockBehavior.Strict);
        Sut = new CategoriesController(CategoryService.Object);
    }
}
