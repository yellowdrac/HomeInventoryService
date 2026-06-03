using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthenticationResponse>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _refreshTokenService = refreshTokenService;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var consumed = await _refreshTokenService.ValidateAndConsumeAsync(request.RefreshToken, cancellationToken);
        if (consumed.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(consumed.Error);
        }

        var user = await _identityService.FindByIdAsync(consumed.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        var accessToken = _tokenService.CreateAccessToken(user);
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return AuthenticationResponse.From(accessToken, refreshToken);
    }
}
