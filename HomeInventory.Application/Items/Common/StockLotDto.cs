namespace HomeInventory.Application.Items.Common;

/// <summary>
/// Read model of a stock lot, enriched with its item name and the location it is stored at
/// (id, name and breadcrumb of names from the root down to the location).
/// </summary>
public sealed record StockLotDto(
    Guid Id,
    Guid ItemId,
    string ItemName,
    Guid LocationId,
    string LocationName,
    IReadOnlyList<string> LocationBreadcrumb,
    decimal Quantity,
    DateOnly? ExpirationDate,
    DateOnly? AcquiredDate);
