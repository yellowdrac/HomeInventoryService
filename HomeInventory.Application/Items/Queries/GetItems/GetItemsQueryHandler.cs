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
    private readonly IFileStorage _fileStorage;

    public GetItemsQueryHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser;
        _context = context;
        _fileStorage = fileStorage;
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

        if (request.BelowMinimum == true)
        {
            // Resolve below-minimum item IDs in two queries, then inject an IN-clause so that
            // subsequent totalCount and pagination operate on the already-filtered set.
            var thresholds = await query
                .Where(i => i.MinimumQuantity != null)
                .Select(i => new { i.Id, i.MinimumQuantity })
                .ToListAsync(cancellationToken);

            if (thresholds.Count == 0)
            {
                return new PagedResult<ItemDto>([], request.Page, request.PageSize, 0);
            }

            var thresholdIds = thresholds.Select(t => t.Id).ToList();

            var stockTotals = await _context.StockLots
                .Where(s => s.HouseholdId == householdId && thresholdIds.Contains(s.ItemId))
                .GroupBy(s => s.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(s => s.Quantity) })
                .ToDictionaryAsync(x => x.ItemId, x => (decimal)x.Total, cancellationToken);

            var belowIds = thresholds
                .Where(t => (stockTotals.TryGetValue(t.Id, out var total) ? total : 0m) < t.MinimumQuantity!.Value)
                .Select(t => t.Id)
                .ToHashSet();

            if (belowIds.Count == 0)
            {
                return new PagedResult<ItemDto>([], request.Page, request.PageSize, 0);
            }

            query = query.Where(i => belowIds.Contains(i.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderBy(i => i.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(i => i.Unit)
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
            .Select(i => i.ToDto(
                totals.TryGetValue(i.Id, out var total) ? total : 0m,
                _fileStorage.GetPresignedReadUrlOrNull(i.PhotoUrl)))
            .ToList();

        return new PagedResult<ItemDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
