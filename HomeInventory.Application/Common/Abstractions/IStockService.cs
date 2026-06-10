using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Centralizes every stock-lot mutation (create / adjust / move / consume / discard). Handlers go
/// through this service instead of touching <see cref="StockLot"/> directly, so each mutation also
/// records the matching <see cref="Movement"/> (append-only history) within the same transaction.
/// <c>PerformedByUserId</c> and <c>OccurredAt</c> are taken from the current user and the clock.
/// Mutations are staged on the context; the caller owns the <c>SaveChanges</c>.
/// </summary>
public interface IStockService
{
    /// <summary>Stages a new stock lot and records a <c>Created</c> movement.</summary>
    StockLot AddLot(
        Guid householdId,
        Guid itemId,
        Guid locationId,
        decimal quantity,
        DateOnly? expirationDate,
        DateOnly? acquiredDate);

    /// <summary>
    /// Adjusts the quantity and dates of an existing lot. Records an <c>Adjusted</c> movement with
    /// the signed quantity delta only when the quantity actually changes (date-only edits do not log).
    /// </summary>
    void AdjustLot(StockLot lot, decimal quantity, DateOnly? expirationDate, DateOnly? acquiredDate);

    /// <summary>
    /// Removes a whole lot and records a <c>Discarded</c> movement for its remaining quantity.
    /// </summary>
    void DiscardLot(StockLot lot);

    /// <summary>
    /// Moves <paramref name="quantity"/> from <paramref name="source"/> to the destination location.
    /// When <paramref name="mergeTarget"/> is supplied (an existing lot of the same item with the same
    /// expiration date at the destination) the quantity is added to it; otherwise a new lot is created
    /// copying the source dates. The source lot is reduced and removed when it reaches zero. Records a
    /// <c>Moved</c> movement. Returns the destination lot.
    /// </summary>
    StockLot Move(StockLot source, Guid toLocationId, decimal quantity, StockLot? mergeTarget);

    /// <summary>
    /// Reduces a lot by <paramref name="quantity"/> and records a <c>Consumed</c> movement. The lot is
    /// removed when it reaches zero.
    /// </summary>
    void Consume(StockLot lot, decimal quantity, string? reason);

    /// <summary>
    /// Reduces a lot by <paramref name="quantity"/> and records a <c>Discarded</c> movement. The lot is
    /// removed when it reaches zero.
    /// </summary>
    void Discard(StockLot lot, decimal quantity, string? reason);
}
