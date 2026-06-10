using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Expirations.Common;
using HomeInventory.Application.Locations.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Expirations.Queries.GetKitchenOverview;

public sealed class GetKitchenOverviewQueryHandler
    : IRequestHandler<GetKitchenOverviewQuery, Result<KitchenOverviewDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetKitchenOverviewQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<KitchenOverviewDto>> Handle(
        GetKitchenOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<KitchenOverviewDto>(HouseholdErrors.NoHousehold);
        }

        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = asOf.AddDays(request.WithinDays);

        var lots = await _context.StockLots
            .Where(s => s.HouseholdId == householdId && s.ExpirationDate != null)
            .ToListAsync(cancellationToken);

        if (request.LocationId is { } locationId)
        {
            var nodes = await _context.Locations
                .Where(l => l.HouseholdId == householdId)
                .ToListAsync(cancellationToken);
            var allowed = LocationSubtree.CollectIds(locationId, nodes);
            lots = lots.Where(l => allowed.Contains(l.LocationId)).ToList();
        }

        var expiredCount = lots.Count(l => l.ExpirationDate!.Value < asOf);
        var expiringSoonCount = lots.Count(l =>
            l.ExpirationDate!.Value >= asOf && l.ExpirationDate.Value <= threshold);
        var soonestExpiration = lots.Count == 0
            ? (DateOnly?)null
            : lots.Min(l => l.ExpirationDate!.Value);

        return new KitchenOverviewDto(expiredCount, expiringSoonCount, lots.Count, soonestExpiration);
    }
}
