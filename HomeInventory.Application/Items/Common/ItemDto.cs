using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Items.Common;

/// <summary>Read model of an item, including the total quantity across all its stock lots.</summary>
public sealed record ItemDto(
    Guid Id,
    string Name,
    string? Category,
    string? Barcode,
    TrackingType TrackingType,
    string? Unit,
    string? PhotoUrl,
    decimal TotalQuantity);
