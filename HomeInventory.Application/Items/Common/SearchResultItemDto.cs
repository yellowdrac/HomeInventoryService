using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Items.Common;

/// <summary>
/// A single search hit: the matched item plus every place it is stored. Items with no stock are
/// still returned, with an empty <see cref="Placements"/> list and a <see cref="TotalQuantity"/> of 0.
/// </summary>
public sealed record SearchResultItemDto(
    Guid ItemId,
    string Name,
    string? Category,
    TrackingType TrackingType,
    string? Unit,
    decimal TotalQuantity,
    IReadOnlyList<SearchPlacementDto> Placements);
