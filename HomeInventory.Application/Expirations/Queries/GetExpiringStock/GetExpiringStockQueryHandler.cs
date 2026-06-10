using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Expirations.Common;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Expirations.Queries.GetExpiringStock;

public sealed class GetExpiringStockQueryHandler
    : IRequestHandler<GetExpiringStockQuery, Result<IReadOnlyList<ExpiringLotDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetExpiringStockQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<IReadOnlyList<ExpiringLotDto>>> Handle(
        GetExpiringStockQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<IReadOnlyList<ExpiringLotDto>>(HouseholdErrors.NoHousehold);
        }

        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = asOf.AddDays(request.WithinDays);

        // Only perishable lots due on or before the window; optionally drop the already-expired ones.
        var lotsQuery = _context.StockLots
            .Where(s => s.HouseholdId == householdId
                && s.ExpirationDate != null
                && s.ExpirationDate <= threshold);
        if (!request.IncludeExpired)
        {
            lotsQuery = lotsQuery.Where(s => s.ExpirationDate >= asOf);
        }

        var lots = await lotsQuery.ToListAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var allowedLocationIds = request.LocationId is { } locationId
            ? LocationSubtree.CollectIds(locationId, locationsById.Values)
            : null;

        var itemIds = lots.Select(l => l.ItemId).Distinct().ToList();
        var itemsById = await _context.Items
            .Where(i => i.HouseholdId == householdId && itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var categoryFilter = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category;

        var dtos = lots
            .Where(l => allowedLocationIds is null || allowedLocationIds.Contains(l.LocationId))
            .Where(l => categoryFilter is null
                || (itemsById.TryGetValue(l.ItemId, out var item) && item.Category == categoryFilter))
            .Select(l => BuildDto(l, asOf, request.WithinDays, itemsById, locationsById))
            // FEFO: earliest expiry first.
            .OrderBy(d => d.ExpirationDate)
            .ThenBy(d => d.ItemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return dtos;
    }

    private static ExpiringLotDto BuildDto(
        StockLot lot,
        DateOnly asOf,
        int withinDays,
        IReadOnlyDictionary<Guid, Item> itemsById,
        IReadOnlyDictionary<Guid, Location> locationsById)
    {
        var expiration = lot.ExpirationDate!.Value;
        var itemName = itemsById.TryGetValue(lot.ItemId, out var item) ? item.Name : string.Empty;
        var locationName = locationsById.TryGetValue(lot.LocationId, out var location)
            ? location.Name
            : string.Empty;

        return new ExpiringLotDto(
            lot.Id,
            lot.ItemId,
            itemName,
            lot.LocationId,
            locationName,
            LocationMappingExtensions.BuildBreadcrumb(lot.LocationId, locationsById),
            lot.Quantity,
            expiration,
            ExpirationEvaluation.DaysUntil(expiration, asOf),
            ExpirationEvaluation.GetStatus(expiration, asOf, withinDays));
    }
}
