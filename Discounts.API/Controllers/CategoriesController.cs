using Discounts.Application.DTOs;
using Discounts.Application.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetActiveCategories(CancellationToken ct)
    {
        var models = await _categoryService.GetActiveCategoriesAsync(ct).ConfigureAwait(false);
        return models.Adapt<List<CategoryDto>>();
    }
}
