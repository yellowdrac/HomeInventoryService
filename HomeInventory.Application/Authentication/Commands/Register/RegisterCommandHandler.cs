using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.Register;

public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            cancellationToken);

        if (registration.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(registration.Error);
        }

        var user = registration.Value;
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(user, refreshToken.ExpiresAtUtc);

        return AuthenticationResponse.From(accessToken, refreshToken);
    }
}
