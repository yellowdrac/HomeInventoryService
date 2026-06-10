namespace HomeInventory.Application.Expirations.Common;

/// <summary>
/// Dashboard summary of perishable stock for a household (optionally scoped to a location subtree):
/// how many lots are expired, how many are due soon, the total perishable lot count and the nearest
/// upcoming expiration date (if any).
/// </summary>
public sealed record KitchenOverviewDto(
    int ExpiredCount,
    int ExpiringSoonCount,
    int PerishableLotCount,
    DateOnly? SoonestExpiration);
