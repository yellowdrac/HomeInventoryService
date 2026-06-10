using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Queries.GetLocationContents;

public sealed class GetLocationContentsQueryHandler
    : IRequestHandler<GetLocationContentsQuery, Result<IReadOnlyList<StockLotDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetLocationContentsQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<IReadOnlyList<StockLotDto>>> Handle(
        GetLocationContentsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<IReadOnlyList<StockLotDto>>(HouseholdErrors.NoHousehold);
        }

        var locationExists = await _context.Locations
            .AnyAsync(
                l => l.Id == request.LocationId && l.HouseholdId == householdId,
                cancellationToken);
        if (!locationExists)
        {
            return Result.Failure<IReadOnlyList<StockLotDto>>(LocationErrors.NotFound);
        }

        var lots = await _context.StockLots
            .Where(s => s.LocationId == request.LocationId && s.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var itemIds = lots.Select(l => l.ItemId).Distinct().ToList();
        var itemsById = await _context.Items
            .Where(i => i.HouseholdId == householdId && itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var dtos = lots
            .Select(lot => StockLotDtoFactory.Build(lot, itemsById, locationsById))
            .ToList();

        return dtos;
    }
}
