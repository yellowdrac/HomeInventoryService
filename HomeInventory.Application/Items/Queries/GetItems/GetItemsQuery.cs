using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Items.Queries.GetItems;

/// <summary>
/// Returns a page of items, optionally filtered by name (accent/case-insensitive) and/or category.
/// Each item carries its <c>TotalQuantity</c> (sum of its stock-lot quantities).
/// </summary>
public sealed record GetItemsQuery(
    string? NameFilter = null,
    string? Category = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<ItemDto>>>;
