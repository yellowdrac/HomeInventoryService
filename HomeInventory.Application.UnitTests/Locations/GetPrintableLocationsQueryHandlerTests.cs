using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Locations.Queries.GetPrintableLocations;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class GetPrintableLocationsQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private readonly Guid _homeId = Guid.NewGuid();
    private readonly Guid _bedroomId = Guid.NewGuid();
    private readonly Guid _wardrobeId = Guid.NewGuid();
    private readonly Guid _kitchenId = Guid.NewGuid();

    private GetPrintableLocationsQueryHandler BuildHandler(List<Location> locations)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetPrintableLocationsQueryHandler(_currentUser, _context);
    }

    private List<Location> Tree() =>
    [
        Node(_homeId, null, "Home"),
        Node(_bedroomId, _homeId, "Bedroom"),
        Node(_wardrobeId, _bedroomId, "Wardrobe"),
        Node(_kitchenId, _homeId, "Kitchen"),
    ];

    private Location Node(Guid id, Guid? parentId, string name) => new()
    {
        Id = id,
        HouseholdId = _householdId,
        ParentId = parentId,
        Name = name,
        Type = LocationType.Room,
        QrSlug = name.ToLowerInvariant(),
    };

    [Fact]
    public async Task Lists_every_location_with_its_slug_and_breadcrumb()
    {
        var handler = BuildHandler(Tree());

        var result = await handler.Handle(
            new GetPrintableLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);

        var wardrobe = result.Value.Single(p => p.Id == _wardrobeId);
        wardrobe.QrSlug.Should().Be("wardrobe");
        wardrobe.Breadcrumb.Should().Be("Home / Bedroom / Wardrobe");
    }

    [Fact]
    public async Task Scopes_to_the_subtree_when_a_location_id_is_given()
    {
        var handler = BuildHandler(Tree());

        var result = await handler.Handle(
            new GetPrintableLocationsQuery(_bedroomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(p => p.Id)
            .Should().BeEquivalentTo([_bedroomId, _wardrobeId]);
    }
}
