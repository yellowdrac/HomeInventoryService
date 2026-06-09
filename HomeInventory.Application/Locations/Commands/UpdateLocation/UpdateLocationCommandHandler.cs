using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Commands.UpdateLocation;

public sealed class UpdateLocationCommandHandler
    : IRequestHandler<UpdateLocationCommand, Result<LocationDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public UpdateLocationCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<LocationDto>> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<LocationDto>(HouseholdErrors.NoHousehold);
        }

        var location = await _context.Locations
            .FirstOrDefaultAsync(
                l => l.Id == request.Id && l.HouseholdId == householdId,
                cancellationToken);

        if (location is null)
        {
            return Result.Failure<LocationDto>(LocationErrors.NotFound);
        }

        location.Name = request.Name;
        location.Type = request.Type;
        location.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return location.ToDto();
    }
}
