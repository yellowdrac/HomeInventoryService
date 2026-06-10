using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Locations.Common;

internal static class LocationMappingExtensions
{
    public static LocationDto ToDto(this Location location) =>
        new(location.Id, location.Name, location.Type, location.ParentId, location.QrSlug);

    /// <summary>
    /// Builds the breadcrumb of names from the root down to <paramref name="locationId"/> (inclusive),
    /// walking the parent chain through <paramref name="byId"/>. Returns an empty list when the
    /// location is not present in the lookup.
    /// </summary>
    public static IReadOnlyList<string> BuildBreadcrumbNames(
        Guid locationId,
        IReadOnlyDictionary<Guid, Location> byId)
    {
        var names = new List<string>();
        var currentId = (Guid?)locationId;

        while (currentId is { } id && byId.TryGetValue(id, out var node))
        {
            names.Add(node.Name);
            currentId = node.ParentId;
        }

        names.Reverse();
        return names;
    }

    /// <summary>
    /// Builds the breadcrumb of <see cref="LocationDto"/> nodes from the root down to
    /// <paramref name="locationId"/> (inclusive), walking the parent chain through
    /// <paramref name="byId"/>. Returns an empty list when the location is not present in the lookup.
    /// </summary>
    public static IReadOnlyList<LocationDto> BuildBreadcrumb(
        Guid locationId,
        IReadOnlyDictionary<Guid, Location> byId)
    {
        var nodes = new List<LocationDto>();
        var currentId = (Guid?)locationId;

        while (currentId is { } id && byId.TryGetValue(id, out var node))
        {
            nodes.Add(node.ToDto());
            currentId = node.ParentId;
        }

        nodes.Reverse();
        return nodes;
    }
}
