namespace HomeInventory.Application.Common.Models;

/// <summary>Read model describing the household of the current user.</summary>
public sealed record HouseholdResponse(
    Guid Id,
    string Name,
    string JoinCode,
    Guid OwnerUserId,
    bool IsOwner);
