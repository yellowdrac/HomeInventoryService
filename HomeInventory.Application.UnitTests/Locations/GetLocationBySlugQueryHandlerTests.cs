using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Locations.Queries.GetLocationBySlug;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class GetLocationBySlugQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private readonly Guid _homeId = Guid.NewGuid();
    private readonly Guid _bedroomId = Guid.NewGuid();
    private readonly Guid _wardrobeId = Guid.NewGuid();

    private GetLocationBySlugQueryHandler BuildHandler(
        List<Location> locations,
        List<Item>? items = null,
        List<StockLot>? stockLots = null)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        var itemsDbSet = (items ?? []).BuildMockDbSet();
        var stockLotsDbSet = (stockLots ?? []).BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.Items.Returns(itemsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetLocationBySlugQueryHandler(_currentUser, _context);
    }

    private List<Location> Chain() =>
    [
        Node(_homeId, null, "Home"),
        Node(_bedroomId, _homeId, "Bedroom"),
        Node(_wardrobeId, _bedroomId, "Wardrobe"),
    ];

    private Location Node(Guid id, Guid? parentId, string name, Guid? householdId = null) => new()
    {
        Id = id,
        HouseholdId = householdId ?? _householdId,
        ParentId = parentId,
        Name = name,
        Type = LocationType.Room,
        QrSlug = name.ToLowerInvariant(),
    };

    [Fact]
    public async Task Resolves_the_slug_to_its_location_with_breadcrumb_and_contents()
    {
        var itemId = Guid.NewGuid();
        var items = new List<Item>
        {
            new() { Id = itemId, HouseholdId = _householdId, Name = "Sweater", NormalizedName = "sweater", TrackingType = TrackingType.Quantity },
        };
        var lots = new List<StockLot>
        {
            new() { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = _wardrobeId, Quantity = 3 },
        };
        var handler = BuildHandler(Chain(), items, lots);

        var result = await handler.Handle(
            new GetLocationBySlugQuery("wardrobe"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Detail.Id.Should().Be(_wardrobeId);
        result.Value.Detail.Breadcrumb.Select(b => b.Name)
            .Should().ContainInOrder("Home", "Bedroom", "Wardrobe");
        result.Value.Contents.Should().ContainSingle();
        result.Value.Contents[0].ItemName.Should().Be("Sweater");
        result.Value.Contents[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Fails_when_the_slug_does_not_exist()
    {
        var handler = BuildHandler(Chain());

        var result = await handler.Handle(
            new GetLocationBySlugQuery("does-not-exist"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }

    [Fact]
    public async Task Does_not_resolve_a_slug_that_belongs_to_another_household()
    {
        // The same slug exists, but only on a node owned by a different household.
        var otherHousehold = new List<Location>
        {
            new()
            {
                Id = Guid.NewGuid(),
                HouseholdId = Guid.NewGuid(),
                ParentId = null,
                Name = "Garage",
                Type = LocationType.Room,
                QrSlug = "shared-slug",
            },
        };
        var handler = BuildHandler(otherHousehold);

        var result = await handler.Handle(
            new GetLocationBySlugQuery("shared-slug"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }
}
