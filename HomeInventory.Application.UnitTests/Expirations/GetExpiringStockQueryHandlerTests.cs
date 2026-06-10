using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Expirations.Common;
using HomeInventory.Application.Expirations.Queries.GetExpiringStock;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Expirations;

public class GetExpiringStockQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly DateOnly _today = new(2026, 6, 10);
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private GetExpiringStockQueryHandler BuildHandler(
        List<Item> items, List<Location> locations, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetExpiringStockQueryHandler(_currentUser, _context);
    }

    private Item Item(Guid id, string name, string? category = null) => new()
    {
        Id = id,
        HouseholdId = _householdId,
        Name = name,
        NormalizedName = name.ToLowerInvariant(),
        Category = category,
        TrackingType = TrackingType.Quantity,
    };

    private StockLot Lot(Guid itemId, Guid locationId, DateOnly? expiration, decimal quantity = 1, Guid? householdId = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = householdId ?? _householdId,
        ItemId = itemId,
        LocationId = locationId,
        Quantity = quantity,
        ExpirationDate = expiration,
    };

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var itemsDbSet = new List<Item>().BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new GetExpiringStockQueryHandler(_currentUser, _context);

        var result = await handler.Handle(new GetExpiringStockQuery(AsOfDate: _today), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }

    [Fact]
    public async Task Orders_fefo_and_skips_lots_without_an_expiration_date()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var soon = Lot(itemId, locationId, _today.AddDays(2));
        var sooner = Lot(itemId, locationId, _today.AddDays(1));
        var noDate = Lot(itemId, locationId, null);
        var handler = BuildHandler([Item(itemId, "Milk")], [location], [soon, sooner, noDate]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(l => l.StockLotId).Should().ContainInOrder(sooner.Id, soon.Id);
        result.Value.Should().NotContain(l => l.StockLotId == noDate.Id);
    }

    [Fact]
    public async Task Computes_status_and_days_until_expiry_relative_to_as_of()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var expired = Lot(itemId, locationId, _today.AddDays(-3));
        var soon = Lot(itemId, locationId, _today.AddDays(2));
        var handler = BuildHandler([Item(itemId, "Yogurt")], [location], [expired, soon]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, IncludeExpired: true, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expiredDto = result.Value.Single(l => l.StockLotId == expired.Id);
        expiredDto.Status.Should().Be(ExpirationStatus.Expired);
        expiredDto.DaysUntilExpiry.Should().Be(-3);

        var soonDto = result.Value.Single(l => l.StockLotId == soon.Id);
        soonDto.Status.Should().Be(ExpirationStatus.ExpiringSoon);
        soonDto.DaysUntilExpiry.Should().Be(2);
    }

    [Fact]
    public async Task Excludes_expired_lots_when_include_expired_is_false()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var expired = Lot(itemId, locationId, _today.AddDays(-1));
        var soon = Lot(itemId, locationId, _today.AddDays(1));
        var handler = BuildHandler([Item(itemId, "Cheese")], [location], [expired, soon]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, IncludeExpired: false, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(l => l.StockLotId).Should().Equal(soon.Id);
    }

    [Fact]
    public async Task Excludes_lots_beyond_the_window()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var within = Lot(itemId, locationId, _today.AddDays(5));
        var beyond = Lot(itemId, locationId, _today.AddDays(20));
        var handler = BuildHandler([Item(itemId, "Ham")], [location], [within, beyond]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(l => l.StockLotId).Should().Equal(within.Id);
    }

    [Fact]
    public async Task Location_filter_includes_the_whole_subtree_with_breadcrumb()
    {
        var itemId = Guid.NewGuid();
        var kitchenId = Guid.NewGuid();
        var fridgeId = Guid.NewGuid();
        var garageId = Guid.NewGuid();
        var locations = new List<Location>
        {
            new() { Id = kitchenId, HouseholdId = _householdId, Name = "Kitchen", Type = LocationType.Zone, QrSlug = "kitchen" },
            new() { Id = fridgeId, HouseholdId = _householdId, ParentId = kitchenId, Name = "Fridge", Type = LocationType.Furniture, QrSlug = "fridge" },
            new() { Id = garageId, HouseholdId = _householdId, Name = "Garage", Type = LocationType.Zone, QrSlug = "garage" },
        };
        var inFridge = Lot(itemId, fridgeId, _today.AddDays(2)); // descendant of Kitchen
        var inGarage = Lot(itemId, garageId, _today.AddDays(2)); // outside the subtree
        var handler = BuildHandler([Item(itemId, "Butter")], locations, [inFridge, inGarage]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, LocationId: kitchenId, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(l => l.StockLotId).Should().Equal(inFridge.Id);
        result.Value.Single().Breadcrumb.Select(b => b.Name).Should().ContainInOrder("Kitchen", "Fridge");
    }

    [Fact]
    public async Task Filters_by_category()
    {
        var dairyId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var items = new List<Item> { Item(dairyId, "Milk", "Dairy"), Item(toolId, "Glue", "Tools") };
        var dairyLot = Lot(dairyId, locationId, _today.AddDays(2));
        var toolLot = Lot(toolId, locationId, _today.AddDays(2));
        var handler = BuildHandler(items, [location], [dairyLot, toolLot]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, Category: "Dairy", AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(l => l.StockLotId).Should().Equal(dairyLot.Id);
    }

    [Fact]
    public async Task Is_scoped_to_the_current_household()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var mine = Lot(itemId, locationId, _today.AddDays(2));
        var theirs = Lot(itemId, locationId, _today.AddDays(2), householdId: Guid.NewGuid());
        var handler = BuildHandler([Item(itemId, "Milk")], [location], [mine, theirs]);

        var result = await handler.Handle(
            new GetExpiringStockQuery(WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(l => l.StockLotId).Should().Equal(mine.Id);
    }
}
