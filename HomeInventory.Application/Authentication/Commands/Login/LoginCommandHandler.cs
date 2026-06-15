using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await _identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (credentials.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(credentials.Error);
        }

        var user = credentials.Value;
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(user, refreshToken.ExpiresAtUtc);

        return AuthenticationResponse.From(accessToken, refreshToken);
    }
}
