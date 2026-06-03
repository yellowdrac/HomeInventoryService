using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the authentication flows.</summary>
public static class AuthenticationErrors
{
    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("Auth.EmailAlreadyInUse", "An account with this email already exists.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid, expired or already used.");

    public static Error RegistrationFailed(string message) =>
        Error.Validation("Auth.RegistrationFailed", message);
}
