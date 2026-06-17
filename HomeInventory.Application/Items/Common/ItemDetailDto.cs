using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Items.Common;

/// <summary>Detailed read model of an item: its fields plus every stock lot it owns.</summary>
public sealed record ItemDetailDto(
    Guid Id,
    string Name,
    string? Category,
    string? Barcode,
    TrackingType TrackingType,
    string? Unit,
    string? PhotoUrl,
    decimal TotalQuantity,
    int? MinimumQuantity,
    IReadOnlyList<StockLotDto> Lots);
