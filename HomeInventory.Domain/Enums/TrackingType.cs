namespace HomeInventory.Domain.Enums;

/// <summary>
/// Tracking strategy for an item: single unique piece or by quantity.
/// </summary>
public enum TrackingType
{
    Unique,
    Quantity,
}
