using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Locations.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Queries.GetLocationBySlug;

public sealed class GetLocationBySlugQueryHandler
    : IRequestHandler<GetLocationBySlugQuery, Result<LocationBySlugDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetLocationBySlugQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<LocationBySlugDto>> Handle(
        GetLocationBySlugQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<LocationBySlugDto>(HouseholdErrors.NoHousehold);
        }

        // Small dataset: load the household nodes once and derive breadcrumb/children in memory.
        var nodes = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        // The slug is scoped to the household, so a slug that only exists in another household
        // is simply not found here.
        var node = nodes.FirstOrDefault(l => l.QrSlug == request.QrSlug);
        if (node is null)
        {
            return Result.Failure<LocationBySlugDto>(LocationErrors.NotFound);
        }

        var byId = nodes.ToDictionary(n => n.Id);

        var breadcrumb = LocationMappingExtensions.BuildBreadcrumb(node.Id, byId);

        var children = nodes
            .Where(l => l.ParentId == node.Id)
            .OrderBy(l => l.Name)
            .Select(l => l.ToDto())
            .ToList();

        var detail = new LocationDetailDto(
            node.Id,
            node.Name,
            node.Type,
            node.ParentId,
            node.QrSlug,
            breadcrumb,
            children);

        // Resolve the contents (stock lots) of the location, reusing the shared lot factory.
        var lots = await _context.StockLots
            .Where(s => s.LocationId == node.Id && s.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var itemIds = lots.Select(l => l.ItemId).Distinct().ToList();
        var itemsById = await _context.Items
            .Where(i => i.HouseholdId == householdId && itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var contents = lots
            .Select(lot => StockLotDtoFactory.Build(lot, itemsById, byId))
            .ToList();

        return new LocationBySlugDto(detail, contents);
    }
}
