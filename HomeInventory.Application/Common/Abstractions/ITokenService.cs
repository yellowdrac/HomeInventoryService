using HomeInventory.Application.Common.Models;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Issues short-lived JWT access tokens. The implementation lives in Infrastructure and
/// reads the signing configuration from settings/user-secrets.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed access token carrying the <c>sub</c>, <c>email</c> and, when present,
    /// <c>householdId</c> claims.
    /// </summary>
    AccessToken CreateAccessToken(AuthUser user);
}
