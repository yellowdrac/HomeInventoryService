namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Contract that exposes the identity of the user and the household (tenant) of the
/// current request. The concrete implementation lives outside Application (Api/Infrastructure);
/// in Phase 1 it will be resolved from the JWT claims.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    /// <summary>
    /// The household the user belongs to, or <c>null</c> when the user has not yet created or
    /// joined one. A null value means scoped data is filtered out by the global query filter.
    /// </summary>
    Guid? HouseholdId { get; }

    /// <summary>
    /// The UTC timestamp at which the entire login session expires. Derived from the
    /// <c>sessionExp</c> JWT claim and never extended by token rotation.
    /// </summary>
    DateTime SessionExpiresAtUtc { get; }
}
