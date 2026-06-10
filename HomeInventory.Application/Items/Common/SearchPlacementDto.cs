using HomeInventory.Application.Locations.Common;

namespace HomeInventory.Application.Items.Common;

/// <summary>
/// One place where a matched item is stored (derived from a stock lot): the location, its
/// breadcrumb (root → location, inclusive), the quantity there and the optional expiration date.
/// </summary>
public sealed record SearchPlacementDto(
    Guid LocationId,
    string LocationName,
    IReadOnlyList<LocationDto> Breadcrumb,
    decimal Quantity,
    DateOnly? ExpirationDate);
