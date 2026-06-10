using HomeInventory.Application.Locations.Common;

namespace HomeInventory.Application.Expirations.Common;

/// <summary>
/// A perishable stock lot (one that has an <c>ExpirationDate</c>) surfaced by the expiration views,
/// enriched with its item, location breadcrumb, days until expiry and expiry status.
/// </summary>
public sealed record ExpiringLotDto(
    Guid StockLotId,
    Guid ItemId,
    string ItemName,
    Guid LocationId,
    string LocationName,
    IReadOnlyList<LocationDto> Breadcrumb,
    decimal Quantity,
    DateOnly ExpirationDate,
    int DaysUntilExpiry,
    ExpirationStatus Status);
