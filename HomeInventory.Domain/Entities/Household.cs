using HomeInventory.Domain.Common;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Household (root tenant). Groups the locations, items and movements of a family.
/// </summary>
public class Household : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    /// <summary>Code that allows other members to join the household.</summary>
    public string JoinCode { get; set; } = string.Empty;
}
