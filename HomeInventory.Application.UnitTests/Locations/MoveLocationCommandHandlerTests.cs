using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Locations.Commands.MoveLocation;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class MoveLocationCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    // A small tree in the current household: root -> child -> grandchild, plus a second root.
    private readonly Guid _rootId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _grandChildId = Guid.NewGuid();
    private readonly Guid _otherRootId = Guid.NewGuid();

    private MoveLocationCommandHandler BuildHandler(List<Location> locations)
    {
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new MoveLocationCommandHandler(_currentUser, _context);
    }

    private List<Location> BuildTree() =>
    [
        Node(_rootId, null, "Root"),
        Node(_childId, _rootId, "Child"),
        Node(_grandChildId, _childId, "GrandChild"),
        Node(_otherRootId, null, "OtherRoot"),
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
    public async Task Moves_a_node_under_a_new_valid_parent()
    {
        var handler = BuildHandler(BuildTree());

        var result = await handler.Handle(
            new MoveLocationCommand(_childId, _otherRootId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(_otherRootId);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Promotes_a_node_to_root_when_the_new_parent_is_null()
    {
        var handler = BuildHandler(BuildTree());

        var result = await handler.Handle(
            new MoveLocationCommand(_childId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Rejects_moving_a_node_into_itself()
    {
        var handler = BuildHandler(BuildTree());

        var result = await handler.Handle(
            new MoveLocationCommand(_rootId, _rootId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.CycleDetected);
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_moving_a_node_into_one_of_its_descendants()
    {
        var handler = BuildHandler(BuildTree());

        // Root cannot move under its grandchild.
        var result = await handler.Handle(
            new MoveLocationCommand(_rootId, _grandChildId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.CycleDetected);
    }

    [Fact]
    public async Task Rejects_a_destination_in_another_household()
    {
        var foreignParentId = Guid.NewGuid();
        var tree = BuildTree();
        tree.Add(new Location
        {
            Id = foreignParentId,
            HouseholdId = Guid.NewGuid(),
            ParentId = null,
            Name = "Foreign",
            Type = LocationType.Zone,
            QrSlug = "foreign",
        });
        var handler = BuildHandler(tree);

        var result = await handler.Handle(
            new MoveLocationCommand(_childId, foreignParentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.ParentNotFound);
    }

    [Fact]
    public async Task Fails_when_the_node_does_not_exist_in_the_household()
    {
        var handler = BuildHandler(BuildTree());

        var result = await handler.Handle(
            new MoveLocationCommand(Guid.NewGuid(), _otherRootId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.NotFound);
    }
}
