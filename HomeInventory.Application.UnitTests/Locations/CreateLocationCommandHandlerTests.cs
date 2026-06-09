using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Locations.Commands.CreateLocation;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class CreateLocationCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IQrSlugGenerator _slugGenerator = Substitute.For<IQrSlugGenerator>();

    private CreateLocationCommandHandler BuildHandler(List<Location> locations)
    {
        // Build the mock DbSet before any other Returns() call (see Households tests).
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Locations.Returns(locationsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new CreateLocationCommandHandler(_currentUser, _context, _slugGenerator);
    }

    [Fact]
    public async Task Creates_a_root_node_scoped_to_the_current_household()
    {
        var handler = BuildHandler([]);
        _slugGenerator.Generate(Arg.Any<string>()).Returns("my-home-abc123");

        var result = await handler.Handle(
            new CreateLocationCommand("My Home", LocationType.Zone, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().BeNull();
        result.Value.QrSlug.Should().Be("my-home-abc123");
        _context.Locations.Received(1).Add(Arg.Is<Location>(l =>
            l.HouseholdId == _householdId && l.Name == "My Home" && l.ParentId == null));
    }

    [Fact]
    public async Task Creates_a_child_node_under_a_parent_of_the_same_household()
    {
        var parentId = Guid.NewGuid();
        var parent = new Location
        {
            Id = parentId,
            HouseholdId = _householdId,
            Name = "My Home",
            Type = LocationType.Zone,
            QrSlug = "my-home-abc123",
        };
        var handler = BuildHandler([parent]);
        _slugGenerator.Generate(Arg.Any<string>()).Returns("bedroom-def456");

        var result = await handler.Handle(
            new CreateLocationCommand("Bedroom", LocationType.Room, parentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(parentId);
        _context.Locations.Received(1).Add(Arg.Is<Location>(l =>
            l.ParentId == parentId && l.Name == "Bedroom"));
    }

    [Fact]
    public async Task Rejects_a_parent_that_belongs_to_another_household()
    {
        var parentId = Guid.NewGuid();
        var foreignParent = new Location
        {
            Id = parentId,
            HouseholdId = Guid.NewGuid(),
            Name = "Foreign",
            Type = LocationType.Zone,
            QrSlug = "foreign-xyz",
        };
        var handler = BuildHandler([foreignParent]);

        var result = await handler.Handle(
            new CreateLocationCommand("Bedroom", LocationType.Room, parentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.ParentNotFound);
        _context.Locations.DidNotReceive().Add(Arg.Any<Location>());
    }

    [Fact]
    public async Task Retries_until_the_qr_slug_is_unique_within_the_household()
    {
        var existing = new Location
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            Name = "Garage",
            Type = LocationType.Zone,
            QrSlug = "shared-slug",
        };
        var handler = BuildHandler([existing]);
        _slugGenerator.Generate(Arg.Any<string>()).Returns("shared-slug", "unique-slug");

        var result = await handler.Handle(
            new CreateLocationCommand("Bedroom", LocationType.Room, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QrSlug.Should().Be("unique-slug");
        _slugGenerator.Received(2).Generate(Arg.Any<string>());
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var handler = BuildHandler([]);
        _currentUser.HouseholdId.Returns((Guid?)null);

        var result = await handler.Handle(
            new CreateLocationCommand("My Home", LocationType.Zone, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
        _context.Locations.DidNotReceive().Add(Arg.Any<Location>());
    }
}
