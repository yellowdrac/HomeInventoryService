using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public DeleteItemCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.Id && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure(ItemErrors.NotFound);
        }

        var hasStock = await _context.StockLots
            .AnyAsync(s => s.ItemId == request.Id && s.HouseholdId == householdId, cancellationToken);
        if (hasStock)
        {
            return Result.Failure(ItemErrors.HasStock);
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
