using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Locations.Common;

internal static class LocationMappingExtensions
{
    public static LocationDto ToDto(this Location location) =>
        new(location.Id, location.Name, location.Type, location.ParentId, location.QrSlug);
}
