using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.AddStock;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class AddStockCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private AddStockCommandHandler BuildHandler(
        List<Item> items, List<Location> locations, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new AddStockCommandHandler(_currentUser, _context, new StockService(_context));
    }

    private Item ItemOf(TrackingType tracking, Guid? householdId = null) => new()
    {
        Id = _itemId,
        HouseholdId = householdId ?? _householdId,
        Name = "Batteries",
        NormalizedName = "batteries",
        TrackingType = tracking,
    };

    private Location LocationOf(Guid? householdId = null) => new()
    {
        Id = _locationId,
        HouseholdId = householdId ?? _householdId,
        Name = "Drawer",
        Type = LocationType.Container,
        QrSlug = "drawer",
    };

    [Fact]
    public async Task Adds_a_lot_for_a_quantity_item()
    {
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], [LocationOf()], []);

        var result = await handler.Handle(
            new AddStockCommand(_itemId, _locationId, 3, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(3);
        result.Value.ItemName.Should().Be("Batteries");
        result.Value.LocationName.Should().Be("Drawer");
        _context.StockLots.Received(1).Add(Arg.Is<StockLot>(s =>
            s.ItemId == _itemId && s.LocationId == _locationId && s.HouseholdId == _householdId));
    }

    [Fact]
    public async Task Rejects_an_item_from_another_household()
    {
        var handler = BuildHandler([ItemOf(TrackingType.Quantity, householdId: Guid.NewGuid())], [LocationOf()], []);

        var result = await handler.Handle(
            new AddStockCommand(_itemId, _locationId, 3, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.ItemNotFound);
        _context.StockLots.DidNotReceive().Add(Arg.Any<StockLot>());
    }

    [Fact]
    public async Task Rejects_a_location_from_another_household()
    {
        var handler = BuildHandler([ItemOf(TrackingType.Quantity)], [LocationOf(householdId: Guid.NewGuid())], []);

        var result = await handler.Handle(
            new AddStockCommand(_itemId, _locationId, 3, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.LocationNotFound);
        _context.StockLots.DidNotReceive().Add(Arg.Any<StockLot>());
    }

    [Fact]
    public async Task Forces_quantity_to_one_for_a_unique_item()
    {
        var handler = BuildHandler([ItemOf(TrackingType.Unique)], [LocationOf()], []);

        // Even though 9 is requested, a unique item gets a single unit.
        var result = await handler.Handle(
            new AddStockCommand(_itemId, _locationId, 9, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task Rejects_a_second_lot_for_a_unique_item()
    {
        var existingLot = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ItemId = _itemId,
            LocationId = _locationId,
            Quantity = 1,
        };
        var handler = BuildHandler([ItemOf(TrackingType.Unique)], [LocationOf()], [existingLot]);

        var result = await handler.Handle(
            new AddStockCommand(_itemId, _locationId, 1, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.UniqueAlreadyStocked);
        _context.StockLots.DidNotReceive().Add(Arg.Any<StockLot>());
    }
}
