using Discounts.API.Controllers;
using Discounts.Application.Interfaces;
using Discounts.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Discounts.Tests.Controllers.Fixtures;

internal sealed class CustomerControllerFixture
{
    public Mock<ICustomerService> CustomerService { get; }
    public CustomerController Sut { get; }

    public CustomerControllerFixture()
    {
        MapsterSetup.EnsureConfigured();

        CustomerService = new Mock<ICustomerService>(MockBehavior.Strict);
        Sut = new CustomerController(CustomerService.Object);
    }

    public void SetAuthenticatedUser(string userId)
    {
        var user = ClaimsPrincipalHelper.CreateAuthenticatedUser(userId, "Customer");
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void SetAnonymousUser()
    {
        var user = ClaimsPrincipalHelper.CreateAnonymousUser();
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
