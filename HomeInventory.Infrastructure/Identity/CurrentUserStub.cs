using HomeInventory.Application.Common.Abstractions;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// Temporary <see cref="ICurrentUser"/> implementation for development: returns
/// a fixed household and user while there is no authentication.
/// </summary>
// TODO Phase 1: replace with JWT claims (resolve UserId/HouseholdId from HttpContext).
public class CurrentUserStub : ICurrentUser
{
    /// <summary>Development household. Matches the data that later phases will seed.</summary>
    public static readonly Guid DevelopmentHouseholdId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static readonly Guid DevelopmentUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public Guid UserId => DevelopmentUserId;

    public Guid? HouseholdId => DevelopmentHouseholdId;
}
