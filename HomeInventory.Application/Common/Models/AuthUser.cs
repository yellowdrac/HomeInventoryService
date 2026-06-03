namespace HomeInventory.Application.Common.Models;

/// <summary>
/// Lightweight projection of an authenticated user, free of any Identity types so the
/// application layer does not depend on Infrastructure.
/// </summary>
public sealed record AuthUser(Guid Id, string Email, string DisplayName, Guid? HouseholdId);
