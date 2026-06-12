using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Queries.GetItemById;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class GetItemByIdQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();

    private GetItemByIdQueryHandler BuildHandler(
        List<Item> items, List<Location> locations, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetItemByIdQueryHandler(_currentUser, _context, _fileStorage);
    }

    [Fact]
    public async Task Returns_the_item_with_its_lots_and_total_quantity()
    {
        var rootId = Guid.NewGuid();
        var drawerId = Guid.NewGuid();
        var item = new Item
        {
            Id = _itemId, HouseholdId = _householdId, Name = "Batteries",
            NormalizedName = "batteries", TrackingType = TrackingType.Quantity, Unit = "unit",
        };
        var locations = new List<Location>
        {
            new() { Id = rootId, HouseholdId = _householdId, Name = "Home", Type = LocationType.Zone, QrSlug = "home" },
            new() { Id = drawerId, HouseholdId = _householdId, ParentId = rootId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
        };
        var lots = new List<StockLot>
        {
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = _itemId, LocationId = drawerId, Quantity = 5 },
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = _itemId, LocationId = drawerId, Quantity = 2 },
        };
        var handler = BuildHandler([item], locations, lots);

        var result = await handler.Handle(new GetItemByIdQuery(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuantity.Should().Be(7);
        result.Value.Lots.Should().HaveCount(2);
        result.Value.Lots[0].LocationBreadcrumb.Should().ContainInOrder("Home", "Drawer");
    }

    [Fact]
    public async Task Orders_lots_fefo_with_undated_lots_last()
    {
        var locationId = Guid.NewGuid();
        var item = new Item
        {
            Id = _itemId, HouseholdId = _householdId, Name = "Yogurt",
            NormalizedName = "yogurt", TrackingType = TrackingType.Quantity, Unit = "unit",
        };
        var location = new Location
        {
            Id = locationId, HouseholdId = _householdId, Name = "Fridge", Type = LocationType.Furniture, QrSlug = "fridge",
        };
        var undated = new StockLot { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = _itemId, LocationId = locationId, Quantity = 1 };
        var later = new StockLot { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = _itemId, LocationId = locationId, Quantity = 1, ExpirationDate = new DateOnly(2026, 8, 1) };
        var sooner = new StockLot { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = _itemId, LocationId = locationId, Quantity = 1, ExpirationDate = new DateOnly(2026, 6, 15) };
        var handler = BuildHandler([item], [location], [undated, later, sooner]);

        var result = await handler.Handle(new GetItemByIdQuery(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Lots.Select(l => l.Id).Should().ContainInOrder(sooner.Id, later.Id, undated.Id);
    }

    [Fact]
    public async Task Fails_when_the_item_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([], [], []);

        var result = await handler.Handle(new GetItemByIdQuery(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.NotFound);
    }

    [Fact]
    public async Task Returns_a_presigned_url_built_from_the_stored_key_when_a_photo_is_set()
    {
        const string key = "households/h/items/i/photo.jpg";
        var item = new Item
        {
            Id = _itemId, HouseholdId = _householdId, Name = "Batteries",
            NormalizedName = "batteries", TrackingType = TrackingType.Quantity, PhotoUrl = key,
        };
        _fileStorage.GetPresignedReadUrl(key, Arg.Any<TimeSpan>()).Returns("https://signed.example/photo");
        var handler = BuildHandler([item], [], []);

        var result = await handler.Handle(new GetItemByIdQuery(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().Be("https://signed.example/photo");
        _fileStorage.Received(1).GetPresignedReadUrl(key, Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task Photo_url_is_null_and_storage_is_not_called_when_there_is_no_photo()
    {
        var item = new Item
        {
            Id = _itemId, HouseholdId = _householdId, Name = "Batteries",
            NormalizedName = "batteries", TrackingType = TrackingType.Quantity,
        };
        var handler = BuildHandler([item], [], []);

        var result = await handler.Handle(new GetItemByIdQuery(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().BeNull();
        _fileStorage.DidNotReceive().GetPresignedReadUrl(Arg.Any<string>(), Arg.Any<TimeSpan>());
    }
}
