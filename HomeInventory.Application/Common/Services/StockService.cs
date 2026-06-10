using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;

namespace HomeInventory.Application.Common.Services;

/// <summary>
/// Single point where stock lots are created, adjusted, moved, consumed and discarded. Every
/// mutation stages the change on the <see cref="StockLot"/> and appends the matching
/// <see cref="Movement"/> on the same context, so a stock change and its history entry are persisted
/// together by the caller's single <c>SaveChanges</c>.
/// </summary>
public sealed class StockService : IStockService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public StockService(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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
        RecordMovement(householdId, itemId, from: null, to: locationId, quantity, MovementType.Created, reason: null);
        return lot;
    }

    public void AdjustLot(StockLot lot, decimal quantity, DateOnly? expirationDate, DateOnly? acquiredDate)
    {
        var delta = quantity - lot.Quantity;

        lot.Quantity = quantity;
        lot.ExpirationDate = expirationDate;
        lot.AcquiredDate = acquiredDate;
        lot.UpdatedAt = DateTime.UtcNow;

        // Date-only edits keep the same quantity; only a real change is worth logging.
        if (delta != 0)
        {
            RecordMovement(
                lot.HouseholdId, lot.ItemId, from: null, to: lot.LocationId, delta, MovementType.Adjusted, reason: null);
        }
    }

    public void DiscardLot(StockLot lot)
    {
        RecordMovement(
            lot.HouseholdId, lot.ItemId, from: lot.LocationId, to: null, lot.Quantity, MovementType.Discarded, reason: null);
        _context.StockLots.Remove(lot);
    }

    public StockLot Move(StockLot source, Guid toLocationId, decimal quantity, StockLot? mergeTarget)
    {
        StockLot destination;
        if (mergeTarget is not null)
        {
            mergeTarget.Quantity += quantity;
            mergeTarget.UpdatedAt = DateTime.UtcNow;
            destination = mergeTarget;
        }
        else
        {
            destination = new StockLot
            {
                Id = Guid.NewGuid(),
                HouseholdId = source.HouseholdId,
                ItemId = source.ItemId,
                LocationId = toLocationId,
                Quantity = quantity,
                ExpirationDate = source.ExpirationDate,
                AcquiredDate = source.AcquiredDate,
                CreatedAt = DateTime.UtcNow,
            };
            _context.StockLots.Add(destination);
        }

        ReduceOrRemove(source, quantity);
        RecordMovement(
            source.HouseholdId, source.ItemId, from: source.LocationId, to: toLocationId, quantity, MovementType.Moved, reason: null);
        return destination;
    }

    public void Consume(StockLot lot, decimal quantity, string? reason)
    {
        ReduceOrRemove(lot, quantity);
        RecordMovement(
            lot.HouseholdId, lot.ItemId, from: lot.LocationId, to: null, quantity, MovementType.Consumed, reason);
    }

    public void Discard(StockLot lot, decimal quantity, string? reason)
    {
        ReduceOrRemove(lot, quantity);
        RecordMovement(
            lot.HouseholdId, lot.ItemId, from: lot.LocationId, to: null, quantity, MovementType.Discarded, reason);
    }

    private void ReduceOrRemove(StockLot lot, decimal quantity)
    {
        lot.Quantity -= quantity;
        if (lot.Quantity <= 0)
        {
            _context.StockLots.Remove(lot);
        }
        else
        {
            lot.UpdatedAt = DateTime.UtcNow;
        }
    }

    private void RecordMovement(
        Guid householdId,
        Guid itemId,
        Guid? from,
        Guid? to,
        decimal quantity,
        MovementType type,
        string? reason)
    {
        var now = DateTime.UtcNow;
        _context.Movements.Add(new Movement
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            ItemId = itemId,
            FromLocationId = from,
            ToLocationId = to,
            Quantity = quantity,
            Type = type,
            Reason = reason,
            PerformedByUserId = _currentUser.UserId,
            OccurredAt = now,
            CreatedAt = now,
        });
    }
}
