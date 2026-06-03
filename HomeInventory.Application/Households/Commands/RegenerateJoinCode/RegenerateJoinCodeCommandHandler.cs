using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Households.Commands.RegenerateJoinCode;

public sealed class RegenerateJoinCodeCommandHandler
    : IRequestHandler<RegenerateJoinCodeCommand, Result<HouseholdResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IJoinCodeGenerator _joinCodeGenerator;

    public RegenerateJoinCodeCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IJoinCodeGenerator joinCodeGenerator)
    {
        _currentUser = currentUser;
        _context = context;
        _joinCodeGenerator = joinCodeGenerator;
    }

    public async Task<Result<HouseholdResponse>> Handle(
        RegenerateJoinCodeCommand request,
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

        if (household.OwnerUserId != _currentUser.UserId)
        {
            return Result.Failure<HouseholdResponse>(HouseholdErrors.NotOwner);
        }

        household.JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
        household.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new HouseholdResponse(
            household.Id,
            household.Name,
            household.JoinCode,
            household.OwnerUserId,
            IsOwner: true);
    }

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        string joinCode;
        do
        {
            joinCode = _joinCodeGenerator.Generate();
        }
        while (await _context.Households.AnyAsync(h => h.JoinCode == joinCode, cancellationToken));

        return joinCode;
    }
}
