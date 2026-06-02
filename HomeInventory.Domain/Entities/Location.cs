using HomeInventory.Domain.Common;
using HomeInventory.Domain.Enums;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Hierarchical location within a household (zone, room, furniture, container, spot).
/// </summary>
public class Location : BaseEntity, IHouseholdScoped
{
    public Guid HouseholdId { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public LocationType Type { get; set; }

    /// <summary>Slug used in the physical QR code that points to this location.</summary>
    public string QrSlug { get; set; } = string.Empty;

    public Location? Parent { get; set; }

    public ICollection<Location> Children { get; set; } = new List<Location>();
}
