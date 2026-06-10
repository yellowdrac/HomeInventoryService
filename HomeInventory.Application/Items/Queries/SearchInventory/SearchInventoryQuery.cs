using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Items.Queries.SearchInventory;

/// <summary>
/// Answers "where is my item?": finds items in the current household whose name matches
/// <paramref name="Query"/> (accent/case-insensitive substring or fuzzy trigram match) or whose
/// barcode equals it, ordered by relevance, and returns every place each item is stored.
/// </summary>
public sealed record SearchInventoryQuery(
    string Query,
    string? Category = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<SearchResultItemDto>>>;
