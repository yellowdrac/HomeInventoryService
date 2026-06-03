namespace HomeInventory.Application.Common.Models;

/// <summary>
/// Pair of tokens returned to the client after a successful authentication flow.
/// </summary>
public sealed record AuthenticationResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc)
{
    public static AuthenticationResponse From(AccessToken accessToken, IssuedRefreshToken refreshToken) =>
        new(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshToken.ExpiresAtUtc);
}
