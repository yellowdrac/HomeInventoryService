using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Wraps ASP.NET Core Identity operations behind an application-owned contract so handlers
/// stay free of Infrastructure types. The concrete implementation lives in Infrastructure.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates a new user. Does not assign a household.</summary>
    Task<Result<AuthUser>> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken);

    /// <summary>Validates the email/password pair and returns the matching user on success.</summary>
    Task<Result<AuthUser>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Returns the user with the given id, or <c>null</c> when it does not exist.</summary>
    Task<AuthUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Assigns (or reassigns) the household the user belongs to.</summary>
    Task<Result> SetHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
}
