using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Commands.AddStock;

public sealed class AddStockCommandHandler : IRequestHandler<AddStockCommand, Result<StockLotDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public AddStockCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result<StockLotDto>> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<StockLotDto>(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure<StockLotDto>(StockErrors.ItemNotFound);
        }

        var location = await _context.Locations
            .FirstOrDefaultAsync(
                l => l.Id == request.LocationId && l.HouseholdId == householdId,
                cancellationToken);
        if (location is null)
        {
            return Result.Failure<StockLotDto>(StockErrors.LocationNotFound);
        }

        var quantity = request.Quantity;
        if (item.TrackingType == TrackingType.Unique)
        {
            var alreadyStocked = await _context.StockLots
                .AnyAsync(s => s.ItemId == item.Id && s.HouseholdId == householdId, cancellationToken);
            if (alreadyStocked)
            {
                return Result.Failure<StockLotDto>(StockErrors.UniqueAlreadyStocked);
            }

            quantity = 1;
        }

        var lot = _stockService.AddLot(
            householdId,
            item.Id,
            location.Id,
            quantity,
            request.ExpirationDate,
            request.AcquiredDate);

        await _context.SaveChangesAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var itemsById = new Dictionary<Guid, Domain.Entities.Item> { [item.Id] = item };

        return StockLotDtoFactory.Build(lot, itemsById, locationsById);
    }
}
