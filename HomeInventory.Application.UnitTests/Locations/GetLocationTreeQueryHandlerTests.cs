using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class GetLocationTreeQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private GetLocationTreeQueryHandler BuildHandler(List<Location> locations)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetLocationTreeQueryHandler(_currentUser, _context);
    }

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
    public async Task Builds_the_nested_tree_from_the_flat_list()
    {
        var rootId = Guid.NewGuid();
        var bedroomId = Guid.NewGuid();
        var wardrobeId = Guid.NewGuid();
        var kitchenId = Guid.NewGuid();

        var handler = BuildHandler(
        [
            Node(rootId, null, "Home"),
            Node(bedroomId, rootId, "Bedroom"),
            Node(kitchenId, rootId, "Kitchen"),
            Node(wardrobeId, bedroomId, "Wardrobe"),
        ]);

        var result = await handler.Handle(new GetLocationTreeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();

        var root = result.Value[0];
        root.Id.Should().Be(rootId);
        // Children come back ordered by name: Bedroom before Kitchen.
        root.Children.Select(c => c.Name).Should().ContainInOrder("Bedroom", "Kitchen");

        var bedroom = root.Children.Single(c => c.Id == bedroomId);
        bedroom.Children.Should().ContainSingle(c => c.Id == wardrobeId);
    }

    [Fact]
    public async Task Returns_every_root_node()
    {
        var firstRoot = Guid.NewGuid();
        var secondRoot = Guid.NewGuid();
        var handler = BuildHandler(
        [
            Node(firstRoot, null, "House A"),
            Node(secondRoot, null, "House B"),
        ]);

        var result = await handler.Handle(new GetLocationTreeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Returns_an_empty_forest_when_there_are_no_locations()
    {
        var handler = BuildHandler([]);

        var result = await handler.Handle(new GetLocationTreeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
