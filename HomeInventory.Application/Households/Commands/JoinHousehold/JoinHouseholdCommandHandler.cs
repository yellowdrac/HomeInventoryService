using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Households.Commands.JoinHousehold;

public sealed class JoinHouseholdCommandHandler
    : IRequestHandler<JoinHouseholdCommand, Result<AuthenticationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public JoinHouseholdCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _currentUser = currentUser;
        _context = context;
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        JoinHouseholdCommand request,
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

        var joinCode = request.JoinCode.Trim().ToUpperInvariant();
        var household = await _context.Households
            .FirstOrDefaultAsync(h => h.JoinCode == joinCode, cancellationToken);

        if (household is null)
        {
            return Result.Failure<AuthenticationResponse>(HouseholdErrors.InvalidJoinCode);
        }

        var assignment = await _identityService.SetHouseholdAsync(userId, household.Id, cancellationToken);
        if (assignment.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(assignment.Error);
        }

        var sessionExpiresAtUtc = _currentUser.SessionExpiresAtUtc;
        var updatedUser = user with { HouseholdId = household.Id };
        var accessToken = _tokenService.CreateAccessToken(updatedUser, sessionExpiresAtUtc);
        var refreshToken = await _refreshTokenService.IssueRotatedAsync(userId, sessionExpiresAtUtc, cancellationToken);

        return AuthenticationResponse.From(accessToken, refreshToken);
    }
}
