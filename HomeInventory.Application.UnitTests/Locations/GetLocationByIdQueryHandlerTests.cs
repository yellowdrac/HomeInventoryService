using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Locations.Queries.GetLocationById;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class GetLocationByIdQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private readonly Guid _homeId = Guid.NewGuid();
    private readonly Guid _bedroomId = Guid.NewGuid();
    private readonly Guid _wardrobeId = Guid.NewGuid();

    private GetLocationByIdQueryHandler BuildHandler(List<Location> locations)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetLocationByIdQueryHandler(_currentUser, _context);
    }

    private List<Location> Chain() =>
    [
        Node(_homeId, null, "Home"),
        Node(_bedroomId, _homeId, "Bedroom"),
        Node(_wardrobeId, _bedroomId, "Wardrobe"),
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
    public async Task Returns_the_breadcrumb_from_root_to_node()
    {
        var handler = BuildHandler(Chain());

        var result = await handler.Handle(
            new GetLocationByIdQuery(_wardrobeId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(_wardrobeId);
        result.Value.Breadcrumb.Select(b => b.Name)
            .Should().ContainInOrder("Home", "Bedroom", "Wardrobe");
    }

    [Fact]
    public async Task Returns_the_direct_children_of_the_node()
    {
        var handler = BuildHandler(Chain());

        var result = await handler.Handle(
            new GetLocationByIdQuery(_bedroomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Breadcrumb.Select(b => b.Name).Should().ContainInOrder("Home", "Bedroom");
        result.Value.Children.Should().ContainSingle(c => c.Id == _wardrobeId);
    }

    [Fact]
    public async Task Fails_when_the_node_does_not_exist_in_the_household()
    {
        var handler = BuildHandler(Chain());

        var result = await handler.Handle(
            new GetLocationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }
}
