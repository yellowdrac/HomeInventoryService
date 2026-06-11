using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Dashboard.Common;
using HomeInventory.Application.Expirations.Common;
using HomeInventory.Application.Movements.Queries.GetMovements;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetDashboardSummaryQueryHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        ISender sender)
    {
        _currentUser = currentUser;
        _context = context;
        _sender = sender;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<DashboardSummaryDto>(HouseholdErrors.NoHousehold);
        }

        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var totalItems = await _context.Items
            .CountAsync(i => i.HouseholdId == householdId, cancellationToken);

        var totalLocations = await _context.Locations
            .CountAsync(l => l.HouseholdId == householdId, cancellationToken);

        var lots = await _context.StockLots
            .Where(s => s.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var totalStockUnits = lots.Sum(l => l.Quantity);

        var perishable = lots.Where(l => l.ExpirationDate != null).ToList();
        var expiredCount = perishable.Count(l =>
            ExpirationEvaluation.GetStatus(l.ExpirationDate!.Value, asOf, request.WithinDays)
                == ExpirationStatus.Expired);
        var expiringSoonCount = perishable.Count(l =>
            ExpirationEvaluation.GetStatus(l.ExpirationDate!.Value, asOf, request.WithinDays)
                == ExpirationStatus.ExpiringSoon);

        // Reuse the movements feature so the recent movements are enriched (item, locations, user)
        // and ordered by OccurredAt descending exactly like the dedicated endpoint.
        var movements = await _sender.Send(
            new GetMovementsQuery(Page: 1, PageSize: request.RecentMovementsCount),
            cancellationToken);

        if (movements.IsFailure)
        {
            return Result.Failure<DashboardSummaryDto>(movements.Error);
        }

        return new DashboardSummaryDto(
            totalItems,
            totalLocations,
            totalStockUnits,
            expiredCount,
            expiringSoonCount,
            movements.Value.Items);
    }
}
