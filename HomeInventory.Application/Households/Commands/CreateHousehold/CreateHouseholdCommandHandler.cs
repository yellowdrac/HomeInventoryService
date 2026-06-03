using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Households.Commands.CreateHousehold;

public sealed class CreateHouseholdCommandHandler
    : IRequestHandler<CreateHouseholdCommand, Result<AuthenticationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IJoinCodeGenerator _joinCodeGenerator;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public CreateHouseholdCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IIdentityService identityService,
        IJoinCodeGenerator joinCodeGenerator,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _currentUser = currentUser;
        _context = context;
        _identityService = identityService;
        _joinCodeGenerator = joinCodeGenerator;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        CreateHouseholdCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not null)
        {
            return Result.Failure<AuthenticationResponse>(HouseholdErrors.AlreadyInHousehold);
        }

        var userId = _currentUser.UserId;
        var user = await _identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(HouseholdErrors.UserNotFound);
        }

        var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);

        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerUserId = userId,
            JoinCode = joinCode,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Households.Add(household);
        await _context.SaveChangesAsync(cancellationToken);

        var assignment = await _identityService.SetHouseholdAsync(userId, household.Id, cancellationToken);
        if (assignment.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(assignment.Error);
        }

        var updatedUser = user with { HouseholdId = household.Id };
        var accessToken = _tokenService.CreateAccessToken(updatedUser);
        var refreshToken = await _refreshTokenService.IssueAsync(userId, cancellationToken);

        return AuthenticationResponse.From(accessToken, refreshToken);
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
