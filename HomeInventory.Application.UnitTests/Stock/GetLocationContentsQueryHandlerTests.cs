using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Stock.Queries.GetLocationContents;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class GetLocationContentsQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private GetLocationContentsQueryHandler BuildHandler(
        List<Item> items, List<Location> locations, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetLocationContentsQueryHandler(_currentUser, _context);
    }

    [Fact]
    public async Task Returns_the_lots_stored_at_the_location_with_item_data()
    {
        var rootId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var locations = new List<Location>
        {
            new() { Id = rootId, HouseholdId = _householdId, Name = "Home", Type = LocationType.Zone, QrSlug = "home" },
            new() { Id = _locationId, HouseholdId = _householdId, ParentId = rootId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
        };
        var items = new List<Item>
        {
            new() { Id = itemId, HouseholdId = _householdId, Name = "Batteries", NormalizedName = "batteries", TrackingType = TrackingType.Quantity },
        };
        var lots = new List<StockLot>
        {
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = _locationId, Quantity = 4 },
        };
        var handler = BuildHandler(items, locations, lots);

        var result = await handler.Handle(new GetLocationContentsQuery(_locationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var lot = result.Value[0];
        lot.ItemName.Should().Be("Batteries");
        lot.LocationName.Should().Be("Drawer");
        lot.LocationBreadcrumb.Should().ContainInOrder("Home", "Drawer");
        lot.Quantity.Should().Be(4);
    }

    [Fact]
    public async Task Fails_when_the_location_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([], [], []);

        var result = await handler.Handle(new GetLocationContentsQuery(_locationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }
}
