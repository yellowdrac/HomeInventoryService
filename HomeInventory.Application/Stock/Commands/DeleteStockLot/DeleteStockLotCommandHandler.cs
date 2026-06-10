using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Commands.DeleteStockLot;

public sealed class DeleteStockLotCommandHandler : IRequestHandler<DeleteStockLotCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public DeleteStockLotCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result> Handle(DeleteStockLotCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var lot = await _context.StockLots
            .FirstOrDefaultAsync(
                s => s.Id == request.Id && s.HouseholdId == householdId,
                cancellationToken);
        if (lot is null)
        {
            return Result.Failure(StockErrors.LotNotFound);
        }

        _stockService.RemoveLot(lot);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
