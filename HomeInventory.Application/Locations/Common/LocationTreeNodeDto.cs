using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Locations.Common;

/// <summary>Recursive read model: a location node together with its nested children.</summary>
public sealed record LocationTreeNodeDto(
    Guid Id,
    string Name,
    LocationType Type,
    Guid? ParentId,
    string QrSlug,
    IReadOnlyList<LocationTreeNodeDto> Children);
