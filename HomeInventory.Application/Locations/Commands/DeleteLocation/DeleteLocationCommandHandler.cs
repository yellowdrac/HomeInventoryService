using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Commands.DeleteLocation;

public sealed class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public DeleteLocationCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var location = await _context.Locations
            .FirstOrDefaultAsync(
                l => l.Id == request.Id && l.HouseholdId == householdId,
                cancellationToken);

        if (location is null)
        {
            return Result.Failure(LocationErrors.NotFound);
        }

        var hasChildren = await _context.Locations
            .AnyAsync(l => l.ParentId == request.Id && l.HouseholdId == householdId, cancellationToken);
        if (hasChildren)
        {
            return Result.Failure(LocationErrors.HasChildren);
        }

        var hasStockLots = await _context.StockLots
            .AnyAsync(s => s.LocationId == request.Id && s.HouseholdId == householdId, cancellationToken);
        if (hasStockLots)
        {
            return Result.Failure(LocationErrors.HasStockLots);
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
