using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Common.Services;

/// <summary>
/// Single point where stock lots are created, adjusted and removed. Phase 4 will extend these
/// methods to record <c>Movement</c> entries; for now they only mutate the stock state.
/// </summary>
public sealed class StockService : IStockService
{
    private readonly IApplicationDbContext _context;

    public StockService(IApplicationDbContext context)
    {
        _context = context;
    }

    public StockLot AddLot(
        Guid householdId,
        Guid itemId,
        Guid locationId,
        decimal quantity,
        DateOnly? expirationDate,
        DateOnly? acquiredDate)
    {
        var lot = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            ItemId = itemId,
            LocationId = locationId,
            Quantity = quantity,
            ExpirationDate = expirationDate,
            AcquiredDate = acquiredDate,
            CreatedAt = DateTime.UtcNow,
        };

        _context.StockLots.Add(lot);
        return lot;
    }

    public void AdjustLot(StockLot lot, decimal quantity, DateOnly? expirationDate, DateOnly? acquiredDate)
    {
        lot.Quantity = quantity;
        lot.ExpirationDate = expirationDate;
        lot.AcquiredDate = acquiredDate;
        lot.UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLot(StockLot lot)
    {
        _context.StockLots.Remove(lot);
    }
}
