using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Items.Common;

/// <summary>
/// Assembles <see cref="StockLotDto"/>s from a lot plus in-memory lookups of the household's
/// items and locations (so the item name, location name and breadcrumb can be resolved).
/// </summary>
internal static class StockLotDtoFactory
{
    public static StockLotDto Build(
        StockLot lot,
        IReadOnlyDictionary<Guid, Item> itemsById,
        IReadOnlyDictionary<Guid, Location> locationsById)
    {
        var itemName = itemsById.TryGetValue(lot.ItemId, out var item) ? item.Name : string.Empty;
        var locationName = locationsById.TryGetValue(lot.LocationId, out var location)
            ? location.Name
            : string.Empty;
        var breadcrumb = LocationMappingExtensions.BuildBreadcrumbNames(lot.LocationId, locationsById);

        return lot.ToDto(itemName, locationName, breadcrumb);
    }
}
