using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Queries.SearchInventory;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class SearchInventoryQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private SearchInventoryQueryHandler BuildHandler(
        List<Item> items,
        List<Location>? locations = null,
        List<StockLot>? stockLots = null)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = (locations ?? []).BuildMockDbSet();
        var stockLotsDbSet = (stockLots ?? []).BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new SearchInventoryQueryHandler(_currentUser, _context);
    }

    private Item Item(
        Guid id,
        string name,
        string normalized,
        string? category = null,
        string? barcode = null,
        Guid? householdId = null) => new()
    {
        Id = id,
        HouseholdId = householdId ?? _householdId,
        Name = name,
        NormalizedName = normalized,
        Category = category,
        Barcode = barcode,
        TrackingType = TrackingType.Quantity,
    };

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var itemsDbSet = new List<Item>().BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new SearchInventoryQueryHandler(_currentUser, _context);

        var result = await handler.Handle(new SearchInventoryQuery("milk"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }

    [Theory]
    [InlineData("platano")]
    [InlineData("Plátano")]
    [InlineData("PLÁTANO")]
    [InlineData("tano")]
    public async Task Finds_item_ignoring_accents_and_case_and_by_substring(string query)
    {
        var bananaId = Guid.NewGuid();
        var handler = BuildHandler(
        [
            Item(bananaId, "Plátano", "platano"),
            Item(Guid.NewGuid(), "Batteries", "batteries"),
        ]);

        var result = await handler.Handle(new SearchInventoryQuery(query), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.ItemId == bananaId);
    }

    [Theory]
    [InlineData("duracel")]   // substring of "duracell"
    [InlineData("duracelll")] // typo: only reachable through trigram similarity
    public async Task Tolerates_typos_via_trigram(string query)
    {
        var duracellId = Guid.NewGuid();
        var handler = BuildHandler(
        [
            Item(duracellId, "Duracell", "duracell"),
            Item(Guid.NewGuid(), "Hammer", "hammer"),
        ]);

        var result = await handler.Handle(new SearchInventoryQuery(query), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.ItemId == duracellId);
    }

    [Fact]
    public async Task Returns_placements_with_breadcrumb_and_sums_total_quantity()
    {
        var itemId = Guid.NewGuid();
        var homeId = Guid.NewGuid();
        var drawerId = Guid.NewGuid();
        var shelfId = Guid.NewGuid();
        var expiration = new DateOnly(2026, 12, 1);

        var locations = new List<Location>
        {
            new() { Id = homeId, HouseholdId = _householdId, Name = "Home", Type = LocationType.Zone, QrSlug = "home" },
            new() { Id = drawerId, HouseholdId = _householdId, ParentId = homeId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
            new() { Id = shelfId, HouseholdId = _householdId, ParentId = homeId, Name = "Shelf", Type = LocationType.Container, QrSlug = "shelf" },
        };
        var stockLots = new List<StockLot>
        {
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = drawerId, Quantity = 4, ExpirationDate = expiration },
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = shelfId, Quantity = 6 },
        };
        var handler = BuildHandler([Item(itemId, "Batteries", "batteries")], locations, stockLots);

        var result = await handler.Handle(new SearchInventoryQuery("batteries"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value.Items.Single(i => i.ItemId == itemId);
        hit.TotalQuantity.Should().Be(10);
        hit.Placements.Should().HaveCount(2);

        var drawerPlacement = hit.Placements.Single(p => p.LocationId == drawerId);
        drawerPlacement.LocationName.Should().Be("Drawer");
        drawerPlacement.Quantity.Should().Be(4);
        drawerPlacement.ExpirationDate.Should().Be(expiration);
        drawerPlacement.Breadcrumb.Select(b => b.Name).Should().ContainInOrder("Home", "Drawer");
    }

    [Fact]
    public async Task Includes_matching_items_without_stock()
    {
        var itemId = Guid.NewGuid();
        var handler = BuildHandler([Item(itemId, "Batteries", "batteries")]);

        var result = await handler.Handle(new SearchInventoryQuery("batteries"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value.Items.Single(i => i.ItemId == itemId);
        hit.Placements.Should().BeEmpty();
        hit.TotalQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Matches_an_exact_barcode()
    {
        var itemId = Guid.NewGuid();
        const string barcode = "7501234567890";
        var handler = BuildHandler(
        [
            Item(itemId, "Imported Cookies", "imported cookies", barcode: barcode),
            Item(Guid.NewGuid(), "Batteries", "batteries"),
        ]);

        var result = await handler.Handle(new SearchInventoryQuery(barcode), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.ItemId == itemId);
    }

    [Fact]
    public async Task Orders_results_by_relevance()
    {
        var exactId = Guid.NewGuid();
        var startsId = Guid.NewGuid();
        var containsId = Guid.NewGuid();
        var similarId = Guid.NewGuid();
        var handler = BuildHandler(
        [
            Item(containsId, "Super Duracell", "super duracell"),
            Item(similarId, "Duracel", "duracel"),
            Item(exactId, "Duracell", "duracell"),
            Item(startsId, "Duracell Plus", "duracell plus"),
        ]);

        var result = await handler.Handle(new SearchInventoryQuery("duracell"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(i => i.ItemId)
            .Should().ContainInOrder(exactId, startsId, containsId, similarId);
    }

    [Fact]
    public async Task Scopes_results_and_quantities_to_the_current_household()
    {
        var otherHousehold = Guid.NewGuid();
        var mineId = Guid.NewGuid();
        var theirsId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var locations = new List<Location>
        {
            new() { Id = locationId, HouseholdId = _householdId, Name = "Fridge", Type = LocationType.Zone, QrSlug = "fridge" },
        };
        var stockLots = new List<StockLot>
        {
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = mineId, LocationId = locationId, Quantity = 2 },
            // Lot for the same item id but owned by another household: must not be summed.
            new() { Id = Guid.NewGuid(), HouseholdId = otherHousehold, ItemId = mineId, LocationId = locationId, Quantity = 99 },
        };
        var handler = BuildHandler(
        [
            Item(mineId, "Milk", "milk"),
            Item(theirsId, "Milk", "milk", householdId: otherHousehold),
        ],
            locations,
            stockLots);

        var result = await handler.Handle(new SearchInventoryQuery("milk"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        var hit = result.Value.Items.Single();
        hit.ItemId.Should().Be(mineId);
        hit.TotalQuantity.Should().Be(2);
    }
}
