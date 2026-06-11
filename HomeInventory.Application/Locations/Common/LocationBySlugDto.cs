using HomeInventory.Application.Items.Common;

namespace HomeInventory.Application.Locations.Common;

/// <summary>
/// Read model returned when resolving a location by its QR slug: the location detail
/// (node, breadcrumb and children) together with its contents (the stock lots stored at it).
/// </summary>
public sealed record LocationBySlugDto(
    LocationDetailDto Detail,
    IReadOnlyList<StockLotDto> Contents);
