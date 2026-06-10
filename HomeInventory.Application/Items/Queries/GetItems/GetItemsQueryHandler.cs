using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Common.Text;
using HomeInventory.Application.Items.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Queries.GetItems;

public sealed class GetItemsQueryHandler
    : IRequestHandler<GetItemsQuery, Result<PagedResult<ItemDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetItemsQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<PagedResult<ItemDto>>> Handle(
        GetItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<PagedResult<ItemDto>>(HouseholdErrors.NoHousehold);
        }

        var query = _context.Items.Where(i => i.HouseholdId == householdId);

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            // Match against the normalized key so the filter ignores case and accents.
            var normalizedFilter = TextNormalization.Normalize(request.NameFilter);
            query = query.Where(i => i.NormalizedName.Contains(normalizedFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(i => i.Category == request.Category);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderBy(i => i.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var itemIds = pageItems.Select(i => i.Id).ToList();

        // Sum the stock quantities of just the items on this page, grouped by item.
        var quantingByItem = await _context.StockLots
            .Where(s => s.HouseholdId == householdId && itemIds.Contains(s.ItemId))
            .GroupBy(s => s.ItemId)
            .Select(g => new { ItemId = g.Key, Total = g.Sum(s => s.Quantity) })
            .ToListAsync(cancellationToken);

        var totals = quantingByItem.ToDictionary(x => x.ItemId, x => x.Total);

        var dtos = pageItems
            .Select(i => i.ToDto(totals.TryGetValue(i.Id, out var total) ? total : 0m))
            .ToList();

        return new PagedResult<ItemDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
