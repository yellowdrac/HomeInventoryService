using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Movements.Common;

/// <summary>
/// Read model of a single movement (append-only history entry), enriched with the item name, the
/// source/destination location names and the display name of the user who performed it.
/// </summary>
public sealed record MovementDto(
    Guid Id,
    Guid ItemId,
    string ItemName,
    Guid? FromLocationId,
    string? FromLocationName,
    Guid? ToLocationId,
    string? ToLocationName,
    decimal Quantity,
    MovementType Type,
    string? Reason,
    Guid PerformedByUserId,
    string PerformedByDisplayName,
    DateTime OccurredAt);
