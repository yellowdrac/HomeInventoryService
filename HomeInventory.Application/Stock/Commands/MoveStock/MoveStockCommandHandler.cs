using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Commands.MoveStock;

public sealed class MoveStockCommandHandler : IRequestHandler<MoveStockCommand, Result<StockLotDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public MoveStockCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result<StockLotDto>> Handle(MoveStockCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<StockLotDto>(HouseholdErrors.NoHousehold);
        }

        var lot = await _context.StockLots
            .FirstOrDefaultAsync(
                s => s.Id == request.StockLotId && s.HouseholdId == householdId,
                cancellationToken);
        if (lot is null)
        {
            return Result.Failure<StockLotDto>(StockErrors.LotNotFound);
        }

        if (request.Quantity > lot.Quantity)
        {
            return Result.Failure<StockLotDto>(StockErrors.InsufficientQuantity);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == lot.ItemId && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure<StockLotDto>(StockErrors.ItemNotFound);
        }

        if (item.TrackingType == TrackingType.Unique && request.Quantity != lot.Quantity)
        {
            return Result.Failure<StockLotDto>(StockErrors.UniqueMustMoveWholeLot);
        }

        if (request.ToLocationId == lot.LocationId)
        {
            return Result.Failure<StockLotDto>(StockErrors.SameLocation);
        }

        var destinationExists = await _context.Locations
            .AnyAsync(
                l => l.Id == request.ToLocationId && l.HouseholdId == householdId,
                cancellationToken);
        if (!destinationExists)
        {
            return Result.Failure<StockLotDto>(StockErrors.LocationNotFound);
        }

        // Merge into an existing lot of the same item and expiration date at the destination.
        var expiration = lot.ExpirationDate;
        var mergeTarget = await _context.StockLots
            .FirstOrDefaultAsync(
                s => s.HouseholdId == householdId
                    && s.LocationId == request.ToLocationId
                    && s.ItemId == lot.ItemId
                    && s.ExpirationDate == expiration,
                cancellationToken);

        var destination = _stockService.Move(lot, request.ToLocationId, request.Quantity, mergeTarget);
        await _context.SaveChangesAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var itemsById = new Dictionary<Guid, Domain.Entities.Item> { [item.Id] = item };

        return StockLotDtoFactory.Build(destination, itemsById, locationsById);
    }
}
