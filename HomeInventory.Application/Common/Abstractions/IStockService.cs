using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Centralizes every stock-lot mutation (create / adjust / remove). Handlers go through this
/// service instead of touching <see cref="StockLot"/> directly, so a future phase can hook
/// movement logging here without rewriting the handlers. Mutations are staged on the context;
/// the caller owns the <c>SaveChanges</c>.
/// </summary>
public interface IStockService
{
    /// <summary>Stages a new stock lot for the given item at the given location.</summary>
    StockLot AddLot(
        Guid householdId,
        Guid itemId,
        Guid locationId,
        decimal quantity,
        DateOnly? expirationDate,
        DateOnly? acquiredDate);

    /// <summary>Adjusts the quantity and dates of an existing lot.</summary>
    void AdjustLot(StockLot lot, decimal quantity, DateOnly? expirationDate, DateOnly? acquiredDate);

    /// <summary>Stages the removal of a lot.</summary>
    void RemoveLot(StockLot lot);
}
