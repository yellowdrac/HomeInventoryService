using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Households.Queries.GetMyHousehold;

public sealed class GetMyHouseholdQueryHandler
    : IRequestHandler<GetMyHouseholdQuery, Result<HouseholdResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetMyHouseholdQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<HouseholdResponse>> Handle(
        GetMyHouseholdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<HouseholdResponse>(HouseholdErrors.NoHousehold);
        }

        var household = await _context.Households
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);

        if (household is null)
        {
            return Result.Failure<HouseholdResponse>(HouseholdErrors.NoHousehold);
        }

        return new HouseholdResponse(
            household.Id,
            household.Name,
            household.JoinCode,
            household.OwnerUserId,
            household.OwnerUserId == _currentUser.UserId);
    }
}
