using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.MoveStock;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class MoveStockCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _lotId = Guid.NewGuid();
    private readonly Guid _fromLocationId = Guid.NewGuid();
    private readonly Guid _toLocationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private MoveStockCommandHandler BuildHandler(
        List<Item> items, List<Location> locations, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        var movementsDbSet = new List<Movement>().BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _context.Movements.Returns(movementsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);
        _currentUser.UserId.Returns(_userId);

        return new MoveStockCommandHandler(_currentUser, _context, new StockService(_context, _currentUser));
    }

    private Item ItemOf(TrackingType tracking) => new()
    {
        Id = _itemId,
        HouseholdId = _householdId,
        Name = "Batteries",
        NormalizedName = "batteries",
        TrackingType = tracking,
    };

    private List<Location> BothLocations(Guid? toHouseholdId = null) =>
    [
        new() { Id = _fromLocationId, HouseholdId = _householdId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
        new() { Id = _toLocationId, HouseholdId = toHouseholdId ?? _householdId, Name = "Shelf", Type = LocationType.Container, QrSlug = "shelf" },
    ];

    private StockLot SourceLot(decimal quantity, DateOnly? expiration = null) => new()
    {
        Id = _lotId,
        HouseholdId = _householdId,
        ItemId = _itemId,
        LocationId = _fromLocationId,
        Quantity = quantity,
        ExpirationDate = expiration,
    };

    [Fact]
    public async Task Partial_move_leaves_both_lots_and_records_moved()
    {
        var source = SourceLot(10);
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 4), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        source.Quantity.Should().Be(6);
        _context.StockLots.DidNotReceive().Remove(Arg.Any<StockLot>());
        // No merge target -> a new lot is created at the destination.
        _context.StockLots.Received(1).Add(Arg.Is<StockLot>(s =>
            s.LocationId == _toLocationId && s.ItemId == _itemId && s.Quantity == 4));
        _context.Movements.Received(1).Add(Arg.Is<Movement>(m =>
            m.Type == MovementType.Moved
            && m.Quantity == 4
            && m.FromLocationId == _fromLocationId
            && m.ToLocationId == _toLocationId
            && m.PerformedByUserId == _userId));
        // The stock change and its movement are persisted together in a single transaction.
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Full_move_empties_and_removes_the_source()
    {
        var source = SourceLot(4);
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 4), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.StockLots.Received(1).Remove(source);
        _context.StockLots.Received(1).Add(Arg.Is<StockLot>(s => s.LocationId == _toLocationId && s.Quantity == 4));
    }

    [Fact]
    public async Task Merges_into_an_existing_destination_lot_with_the_same_expiration()
    {
        var expiration = new DateOnly(2027, 5, 1);
        var source = SourceLot(10, expiration);
        var destination = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ItemId = _itemId,
            LocationId = _toLocationId,
            Quantity = 2,
            ExpirationDate = expiration,
        };
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source, destination]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        destination.Quantity.Should().Be(5);
        source.Quantity.Should().Be(7);
        // Merge -> no new lot created at the destination.
        _context.StockLots.DidNotReceive().Add(Arg.Any<StockLot>());
    }

    [Fact]
    public async Task Creates_a_new_lot_when_the_expiration_differs()
    {
        var source = SourceLot(10, new DateOnly(2027, 5, 1));
        var destination = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ItemId = _itemId,
            LocationId = _toLocationId,
            Quantity = 2,
            ExpirationDate = new DateOnly(2028, 1, 1),
        };
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source, destination]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        destination.Quantity.Should().Be(2);
        _context.StockLots.Received(1).Add(Arg.Is<StockLot>(s =>
            s.LocationId == _toLocationId && s.Quantity == 3 && s.ExpirationDate == new DateOnly(2027, 5, 1)));
    }

    [Fact]
    public async Task Rejects_moving_to_the_same_location()
    {
        var source = SourceLot(10);
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _fromLocationId, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.SameLocation);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }

    [Fact]
    public async Task Rejects_a_quantity_above_the_available()
    {
        var source = SourceLot(3);
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], BothLocations(), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.InsufficientQuantity);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }

    [Fact]
    public async Task Rejects_a_destination_from_another_household()
    {
        var source = SourceLot(10);
        var handler = BuildHandler(
            [ItemOf(TrackingType.Quantity)], BothLocations(toHouseholdId: Guid.NewGuid()), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.LocationNotFound);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }

    [Fact]
    public async Task Requires_moving_the_whole_lot_for_a_unique_item()
    {
        var source = SourceLot(1);
        var handler = BuildHandler([ItemOf(TrackingType.Unique)], BothLocations(), [source]);

        var result = await handler.Handle(
            new MoveStockCommand(_lotId, _toLocationId, 0.5m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.UniqueMustMoveWholeLot);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }
}
