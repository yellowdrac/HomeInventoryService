using HomeInventory.Domain.Common;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Stock lot of an item at a specific location, with its quantity and dates.
/// </summary>
public class StockLot : BaseEntity, IHouseholdScoped
{
    public Guid HouseholdId { get; set; }

    public Guid ItemId { get; set; }

    public Guid LocationId { get; set; }

    public decimal Quantity { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public DateOnly? AcquiredDate { get; set; }
}
