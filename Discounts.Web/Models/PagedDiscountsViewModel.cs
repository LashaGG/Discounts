using Discounts.Application.DTOs;

namespace Discounts.Web.Models;

/// <summary>
/// ViewModel for paginated discount listings in MVC views.
/// </summary>
public class PagedDiscountsViewModel
{
    public IReadOnlyList<DiscountDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public int? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
}
