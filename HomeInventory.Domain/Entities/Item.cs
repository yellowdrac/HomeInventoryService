using HomeInventory.Domain.Common;
using HomeInventory.Domain.Enums;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Inventory item. The actual stock lives in <see cref="StockLot"/>.
/// </summary>
public class Item : BaseEntity, IHouseholdScoped
{
    public Guid HouseholdId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized name (lowercase, without accents) used as a search key that is
    /// insensitive to case and accents.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Barcode { get; set; }

    public string? PhotoUrl { get; set; }

    public TrackingType TrackingType { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    /// <summary>
    /// Alert threshold: triggers a "running low" warning when total stock falls below this value.
    /// Null means no threshold is set. Only meaningful for Quantity-tracked items.
    /// </summary>
    public int? MinimumQuantity { get; set; }
}
