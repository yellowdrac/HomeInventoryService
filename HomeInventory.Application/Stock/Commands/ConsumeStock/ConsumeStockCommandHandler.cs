using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Stock.Commands.ConsumeStock;

public sealed class ConsumeStockCommandHandler : IRequestHandler<ConsumeStockCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;

    public ConsumeStockCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IStockService stockService)
    {
        _currentUser = currentUser;
        _context = context;
        _stockService = stockService;
    }

    public async Task<Result> Handle(ConsumeStockCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var lot = await _context.StockLots
            .FirstOrDefaultAsync(
                s => s.Id == request.StockLotId && s.HouseholdId == householdId,
                cancellationToken);
        if (lot is null)
        {
            return Result.Failure(StockErrors.LotNotFound);
        }

        if (request.Quantity > lot.Quantity)
        {
            return Result.Failure(StockErrors.InsufficientQuantity);
        }

        _stockService.Consume(lot, request.Quantity, request.Reason);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
