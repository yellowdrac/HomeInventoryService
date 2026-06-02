using HomeInventory.Domain.Common;
using HomeInventory.Domain.Enums;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Immutable record of an inventory movement (creation, transfer, consumption, etc.).
/// </summary>
public class Movement : BaseEntity, IHouseholdScoped
{
    public Guid HouseholdId { get; set; }

    public Guid ItemId { get; set; }

    public Guid? FromLocationId { get; set; }

    public Guid? ToLocationId { get; set; }

    public decimal Quantity { get; set; }

    public MovementType Type { get; set; }

    public string? Reason { get; set; }

    public Guid PerformedByUserId { get; set; }

    public DateTime OccurredAt { get; set; }
}
