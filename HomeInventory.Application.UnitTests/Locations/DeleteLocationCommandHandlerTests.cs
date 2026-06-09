using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Locations.Commands.DeleteLocation;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class DeleteLocationCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private DeleteLocationCommandHandler BuildHandler(
        List<Location> locations, List<StockLot> stockLots)
    {
        // Build both mock DbSets before any other Returns() call.
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new DeleteLocationCommandHandler(_currentUser, _context);
    }

    private Location Target() => new()
    {
        Id = _locationId,
        HouseholdId = _householdId,
        Name = "Pantry",
        Type = LocationType.Furniture,
        QrSlug = "pantry",
    };

    [Fact]
    public async Task Deletes_an_empty_leaf_location()
    {
        var location = Target();
        var handler = BuildHandler([location], []);

        var result = await handler.Handle(
            new DeleteLocationCommand(_locationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.Locations.Received(1).Remove(location);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_deletion_when_the_node_has_children()
    {
        var child = new Location
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ParentId = _locationId,
            Name = "Shelf",
            Type = LocationType.Container,
            QrSlug = "shelf",
        };
        var handler = BuildHandler([Target(), child], []);

        var result = await handler.Handle(
            new DeleteLocationCommand(_locationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.HasChildren);
        _context.Locations.DidNotReceive().Remove(Arg.Any<Location>());
    }

    [Fact]
    public async Task Rejects_deletion_when_the_node_holds_stock_lots()
    {
        var stockLot = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ItemId = Guid.NewGuid(),
            LocationId = _locationId,
            Quantity = 3,
        };
        var handler = BuildHandler([Target()], [stockLot]);

        var result = await handler.Handle(
            new DeleteLocationCommand(_locationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.HasStockLots);
        _context.Locations.DidNotReceive().Remove(Arg.Any<Location>());
    }

    [Fact]
    public async Task Fails_when_the_node_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([], []);

        var result = await handler.Handle(
            new DeleteLocationCommand(_locationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }
}
