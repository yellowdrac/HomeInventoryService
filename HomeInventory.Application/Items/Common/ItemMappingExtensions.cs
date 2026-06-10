using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Items.Common;

internal static class ItemMappingExtensions
{
    public static ItemDto ToDto(this Item item, decimal totalQuantity) =>
        new(
            item.Id,
            item.Name,
            item.Category,
            item.Barcode,
            item.TrackingType,
            item.Unit,
            item.PhotoUrl,
            totalQuantity);

    public static StockLotDto ToDto(
        this StockLot lot,
        string itemName,
        string locationName,
        IReadOnlyList<string> locationBreadcrumb) =>
        new(
            lot.Id,
            lot.ItemId,
            itemName,
            lot.LocationId,
            locationName,
            locationBreadcrumb,
            lot.Quantity,
            lot.ExpirationDate,
            lot.AcquiredDate);
}
