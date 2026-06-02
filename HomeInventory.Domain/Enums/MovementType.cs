namespace HomeInventory.Domain.Enums;

/// <summary>
/// Nature of an inventory movement.
/// </summary>
public enum MovementType
{
    Created,
    Moved,
    Consumed,
    Adjusted,
    Discarded,
}
