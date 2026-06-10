using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Expirations.Commands.DiscardExpiredStock;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Expirations;

public class DiscardExpiredStockCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateOnly _today = new(2026, 6, 10);
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private DiscardExpiredStockCommandHandler BuildHandler(List<Location> locations, List<StockLot> stockLots)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        var movementsDbSet = new List<Movement>().BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _context.Movements.Returns(movementsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);
        _currentUser.UserId.Returns(_userId);

        return new DiscardExpiredStockCommandHandler(
            _currentUser, _context, new StockService(_context, _currentUser));
    }

    private StockLot Lot(Guid locationId, DateOnly? expiration, decimal quantity = 2) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = _householdId,
        ItemId = Guid.NewGuid(),
        LocationId = locationId,
        Quantity = quantity,
        ExpirationDate = expiration,
    };

    [Fact]
    public async Task Discards_every_expired_lot_and_records_one_movement_each()
    {
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var expired1 = Lot(locationId, _today.AddDays(-1));
        var expired2 = Lot(locationId, _today.AddDays(-10));
        var fresh = Lot(locationId, _today.AddDays(3));
        var noDate = Lot(locationId, null);
        var handler = BuildHandler([location], [expired1, expired2, fresh, noDate]);

        var result = await handler.Handle(
            new DiscardExpiredStockCommand(AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        _context.StockLots.Received(1).Remove(expired1);
        _context.StockLots.Received(1).Remove(expired2);
        _context.StockLots.DidNotReceive().Remove(fresh);
        _context.StockLots.DidNotReceive().Remove(noDate);
        _context.Movements.Received(2).Add(Arg.Is<Movement>(m => m.Type == MovementType.Discarded));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scopes_the_discard_to_the_location_subtree()
    {
        var kitchenId = Guid.NewGuid();
        var fridgeId = Guid.NewGuid();
        var garageId = Guid.NewGuid();
        var locations = new List<Location>
        {
            new() { Id = kitchenId, HouseholdId = _householdId, Name = "Kitchen", Type = LocationType.Zone, QrSlug = "kitchen" },
            new() { Id = fridgeId, HouseholdId = _householdId, ParentId = kitchenId, Name = "Fridge", Type = LocationType.Furniture, QrSlug = "fridge" },
            new() { Id = garageId, HouseholdId = _householdId, Name = "Garage", Type = LocationType.Zone, QrSlug = "garage" },
        };
        var inSubtree = Lot(fridgeId, _today.AddDays(-1));
        var outside = Lot(garageId, _today.AddDays(-1));
        var handler = BuildHandler(locations, [inSubtree, outside]);

        var result = await handler.Handle(
            new DiscardExpiredStockCommand(LocationId: kitchenId, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        _context.StockLots.Received(1).Remove(inSubtree);
        _context.StockLots.DidNotReceive().Remove(outside);
    }

    [Fact]
    public async Task Does_nothing_when_there_are_no_expired_lots()
    {
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var handler = BuildHandler([location], [Lot(locationId, _today.AddDays(3))]);

        var result = await handler.Handle(
            new DiscardExpiredStockCommand(AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var stockLotsDbSet = new List<StockLot>().BuildMockDbSet();
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new DiscardExpiredStockCommandHandler(
            _currentUser, _context, new StockService(_context, _currentUser));

        var result = await handler.Handle(
            new DiscardExpiredStockCommand(AsOfDate: _today), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
