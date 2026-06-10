using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.UpdateStockLot;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class UpdateStockLotCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _lotId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private UpdateStockLotCommandHandler BuildHandler(
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

        return new UpdateStockLotCommandHandler(_currentUser, _context, new StockService(_context, _currentUser));
    }

    private List<Location> SingleLocation() =>
    [
        new() { Id = _locationId, HouseholdId = _householdId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
    ];

    private StockLot Lot(decimal quantity) => new()
    {
        Id = _lotId,
        HouseholdId = _householdId,
        ItemId = _itemId,
        LocationId = _locationId,
        Quantity = quantity,
    };

    [Fact]
    public async Task Adjusts_quantity_and_dates_of_a_quantity_item_lot()
    {
        var item = new Item { Id = _itemId, HouseholdId = _householdId, Name = "Batteries", NormalizedName = "batteries", TrackingType = TrackingType.Quantity };
        var lot = Lot(2);
        var handler = BuildHandler([item], SingleLocation(), [lot]);
        var expiration = new DateOnly(2027, 1, 1);

        var result = await handler.Handle(
            new UpdateStockLotCommand(_lotId, 8, expiration, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(8);
        result.Value.ExpirationDate.Should().Be(expiration);
        lot.Quantity.Should().Be(8);
        // Retrofit: a quantity change records an Adjusted movement with the signed delta (8 - 2 = 6).
        _context.Movements.Received(1).Add(Arg.Is<Movement>(m =>
            m.Type == MovementType.Adjusted
            && m.Quantity == 6
            && m.ItemId == _itemId
            && m.PerformedByUserId == _userId));
    }

    [Fact]
    public async Task Does_not_record_a_movement_when_only_dates_change()
    {
        var item = new Item { Id = _itemId, HouseholdId = _householdId, Name = "Batteries", NormalizedName = "batteries", TrackingType = TrackingType.Quantity };
        var lot = Lot(5);
        var handler = BuildHandler([item], SingleLocation(), [lot]);

        var result = await handler.Handle(
            new UpdateStockLotCommand(_lotId, 5, new DateOnly(2027, 1, 1), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }

    [Fact]
    public async Task Forces_quantity_to_one_for_a_unique_item_lot()
    {
        var item = new Item { Id = _itemId, HouseholdId = _householdId, Name = "Drill", NormalizedName = "drill", TrackingType = TrackingType.Unique };
        var handler = BuildHandler([item], SingleLocation(), [Lot(1)]);

        var result = await handler.Handle(
            new UpdateStockLotCommand(_lotId, 5, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task Fails_when_the_lot_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([], SingleLocation(), []);

        var result = await handler.Handle(
            new UpdateStockLotCommand(_lotId, 3, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.LotNotFound);
    }
}
