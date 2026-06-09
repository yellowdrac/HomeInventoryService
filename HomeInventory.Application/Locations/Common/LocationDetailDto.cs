using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Locations.Common;

/// <summary>
/// Detailed read model of a node: the node itself, its breadcrumb (root → node, inclusive)
/// and its direct children.
/// </summary>
public sealed record LocationDetailDto(
    Guid Id,
    string Name,
    LocationType Type,
    Guid? ParentId,
    string QrSlug,
    IReadOnlyList<LocationDto> Breadcrumb,
    IReadOnlyList<LocationDto> Children);
