using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Expirations.Queries.GetKitchenOverview;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Expirations;

public class GetKitchenOverviewQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly DateOnly _today = new(2026, 6, 10);
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private GetKitchenOverviewQueryHandler BuildHandler(List<Location> locations, List<StockLot> stockLots)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetKitchenOverviewQueryHandler(_currentUser, _context);
    }

    private StockLot Lot(Guid locationId, DateOnly? expiration, Guid? householdId = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = householdId ?? _householdId,
        ItemId = Guid.NewGuid(),
        LocationId = locationId,
        Quantity = 1,
        ExpirationDate = expiration,
    };

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var stockLotsDbSet = new List<StockLot>().BuildMockDbSet();
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new GetKitchenOverviewQueryHandler(_currentUser, _context);

        var result = await handler.Handle(new GetKitchenOverviewQuery(AsOfDate: _today), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }

    [Fact]
    public async Task Counts_expired_and_expiring_soon_and_reports_soonest()
    {
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var lots = new List<StockLot>
        {
            Lot(locationId, _today.AddDays(-5)), // expired
            Lot(locationId, _today.AddDays(-1)), // expired
            Lot(locationId, _today.AddDays(3)),  // soon
            Lot(locationId, _today.AddDays(30)), // ok, but still perishable
            Lot(locationId, null),               // not perishable, excluded entirely
        };
        var handler = BuildHandler([location], lots);

        var result = await handler.Handle(
            new GetKitchenOverviewQuery(WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiredCount.Should().Be(2);
        result.Value.ExpiringSoonCount.Should().Be(1);
        result.Value.PerishableLotCount.Should().Be(4);
        result.Value.SoonestExpiration.Should().Be(_today.AddDays(-5));
    }

    [Fact]
    public async Task Returns_zeros_and_null_soonest_when_nothing_is_perishable()
    {
        var locationId = Guid.NewGuid();
        var location = new Location { Id = locationId, HouseholdId = _householdId, Name = "Pantry", Type = LocationType.Zone, QrSlug = "pantry" };
        var handler = BuildHandler([location], [Lot(locationId, null)]);

        var result = await handler.Handle(
            new GetKitchenOverviewQuery(AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PerishableLotCount.Should().Be(0);
        result.Value.ExpiredCount.Should().Be(0);
        result.Value.ExpiringSoonCount.Should().Be(0);
        result.Value.SoonestExpiration.Should().BeNull();
    }

    [Fact]
    public async Task Scopes_counts_to_the_location_subtree()
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
        var lots = new List<StockLot>
        {
            Lot(fridgeId, _today.AddDays(-1)), // expired, inside subtree
            Lot(garageId, _today.AddDays(-1)), // expired, outside subtree
        };
        var handler = BuildHandler(locations, lots);

        var result = await handler.Handle(
            new GetKitchenOverviewQuery(LocationId: kitchenId, WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PerishableLotCount.Should().Be(1);
        result.Value.ExpiredCount.Should().Be(1);
    }
}
