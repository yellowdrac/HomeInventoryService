using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Locations.Common;

/// <summary>Flat read model of a single location node.</summary>
public sealed record LocationDto(
    Guid Id,
    string Name,
    LocationType Type,
    Guid? ParentId,
    string QrSlug);
