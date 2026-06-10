using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Expirations.Commands.DiscardExpiredStock;

public sealed class DiscardExpiredStockCommandHandler
    : IRequestHandler<DiscardExpiredStockCommand, Result<int>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public DiscardExpiredStockCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result<int>> Handle(
        DiscardExpiredStockCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<int>(HouseholdErrors.NoHousehold);
        }

        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var expiredLots = await _context.StockLots
            .Where(s => s.HouseholdId == householdId
                && s.ExpirationDate != null
                && s.ExpirationDate < asOf)
            .ToListAsync(cancellationToken);

        if (request.LocationId is { } locationId)
        {
            var nodes = await _context.Locations
                .Where(l => l.HouseholdId == householdId)
                .ToListAsync(cancellationToken);
            var allowed = LocationSubtree.CollectIds(locationId, nodes);
            expiredLots = expiredLots.Where(l => allowed.Contains(l.LocationId)).ToList();
        }

        foreach (var lot in expiredLots)
        {
            _stockService.DiscardLot(lot);
        }

        if (expiredLots.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return expiredLots.Count;
    }
}
