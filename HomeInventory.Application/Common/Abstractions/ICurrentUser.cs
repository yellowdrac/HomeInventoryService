namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Contract that exposes the identity of the user and the household (tenant) of the
/// current request. The concrete implementation lives outside Application (Api/Infrastructure);
/// in Phase 1 it will be resolved from the JWT claims.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    Guid HouseholdId { get; }
}
