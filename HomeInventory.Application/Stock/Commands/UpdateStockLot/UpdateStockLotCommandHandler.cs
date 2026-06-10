using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Commands.UpdateStockLot;

public sealed class UpdateStockLotCommandHandler
    : IRequestHandler<UpdateStockLotCommand, Result<StockLotDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public UpdateStockLotCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result<StockLotDto>> Handle(
        UpdateStockLotCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<StockLotDto>(HouseholdErrors.NoHousehold);
        }

        var lot = await _context.StockLots
            .FirstOrDefaultAsync(
                s => s.Id == request.Id && s.HouseholdId == householdId,
                cancellationToken);
        if (lot is null)
        {
            return Result.Failure<StockLotDto>(StockErrors.LotNotFound);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == lot.ItemId && i.HouseholdId == householdId,
                cancellationToken);

        // A unique-tracked item always holds exactly one unit.
        var quantity = item?.TrackingType == TrackingType.Unique ? 1m : request.Quantity;

        _stockService.AdjustLot(lot, quantity, request.ExpirationDate, request.AcquiredDate);
        await _context.SaveChangesAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var itemsById = item is null
            ? new Dictionary<Guid, Domain.Entities.Item>()
            : new Dictionary<Guid, Domain.Entities.Item> { [item.Id] = item };

        return StockLotDtoFactory.Build(lot, itemsById, locationsById);
    }
}
