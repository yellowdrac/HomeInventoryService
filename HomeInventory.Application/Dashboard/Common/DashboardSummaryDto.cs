using HomeInventory.Application.Movements.Common;

namespace HomeInventory.Application.Dashboard.Common;

/// <summary>
/// Home overview for a household: headline counts (items, locations, total stock units), the
/// expired and expiring-soon perishable lot counts (reusing the expiration math of the expirations
/// feature) and the most recent movements (reusing <see cref="MovementDto"/> from the movements
/// feature).
/// </summary>
public sealed record DashboardSummaryDto(
    int TotalItems,
    int TotalLocations,
    decimal TotalStockUnits,
    int ExpiredCount,
    int ExpiringSoonCount,
    IReadOnlyList<MovementDto> RecentMovements);
