using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Movements.Queries.GetMovements;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Movements;

public class GetMovementsQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemA = Guid.NewGuid();
    private readonly Guid _itemB = Guid.NewGuid();
    private readonly Guid _locationA = Guid.NewGuid();
    private readonly Guid _locationB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    private GetMovementsQueryHandler BuildHandler(List<Movement> movements)
    {
        var items = new List<Item>
        {
            new() { Id = _itemA, HouseholdId = _householdId, Name = "Batteries", NormalizedName = "batteries" },
            new() { Id = _itemB, HouseholdId = _householdId, Name = "Bread", NormalizedName = "bread" },
        };
        var locations = new List<Location>
        {
            new() { Id = _locationA, HouseholdId = _householdId, Name = "Drawer", Type = LocationType.Container, QrSlug = "drawer" },
            new() { Id = _locationB, HouseholdId = _householdId, Name = "Shelf", Type = LocationType.Container, QrSlug = "shelf" },
        };

        var movementsDbSet = movements.BuildMockDbSet();
        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        _context.Movements.Returns(movementsDbSet);
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);
        _identityService.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new AuthUser(_userId, "u@example.com", "Alice", _householdId));

        return new GetMovementsQueryHandler(_currentUser, _context, _identityService);
    }

    private Movement Mv(
        Guid itemId,
        MovementType type,
        DateTime occurredAt,
        Guid? from = null,
        Guid? to = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = _householdId,
        ItemId = itemId,
        FromLocationId = from,
        ToLocationId = to,
        Quantity = 1,
        Type = type,
        PerformedByUserId = _userId,
        OccurredAt = occurredAt,
    };

    [Fact]
    public async Task Orders_by_occurred_at_descending_and_enriches_names()
    {
        var older = Mv(_itemA, MovementType.Created, new DateTime(2026, 1, 1), to: _locationA);
        var newer = Mv(_itemA, MovementType.Consumed, new DateTime(2026, 6, 1), from: _locationA);
        var handler = BuildHandler([older, newer]);

        var result = await handler.Handle(new GetMovementsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(newer.Id);
        result.Value.Items[1].Id.Should().Be(older.Id);
        result.Value.Items[0].ItemName.Should().Be("Batteries");
        result.Value.Items[0].FromLocationName.Should().Be("Drawer");
        result.Value.Items[0].PerformedByDisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task Filters_by_item()
    {
        var handler = BuildHandler(
        [
            Mv(_itemA, MovementType.Created, new DateTime(2026, 1, 1)),
            Mv(_itemB, MovementType.Created, new DateTime(2026, 2, 1)),
        ]);

        var result = await handler.Handle(new GetMovementsQuery(ItemId: _itemA), CancellationToken.None);

        result.Value.Items.Should().ContainSingle().Which.ItemId.Should().Be(_itemA);
    }

    [Fact]
    public async Task Filters_by_location_matching_either_from_or_to()
    {
        var handler = BuildHandler(
        [
            Mv(_itemA, MovementType.Moved, new DateTime(2026, 1, 1), from: _locationA, to: _locationB),
            Mv(_itemA, MovementType.Created, new DateTime(2026, 2, 1), to: _locationB),
            Mv(_itemA, MovementType.Consumed, new DateTime(2026, 3, 1), from: _locationB),
        ]);

        var result = await handler.Handle(new GetMovementsQuery(LocationId: _locationA), CancellationToken.None);

        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].FromLocationId.Should().Be(_locationA);
    }

    [Fact]
    public async Task Filters_by_type()
    {
        var handler = BuildHandler(
        [
            Mv(_itemA, MovementType.Created, new DateTime(2026, 1, 1)),
            Mv(_itemA, MovementType.Discarded, new DateTime(2026, 2, 1)),
        ]);

        var result = await handler.Handle(
            new GetMovementsQuery(Type: MovementType.Discarded), CancellationToken.None);

        result.Value.Items.Should().ContainSingle().Which.Type.Should().Be(MovementType.Discarded);
    }

    [Fact]
    public async Task Filters_by_date_range()
    {
        var handler = BuildHandler(
        [
            Mv(_itemA, MovementType.Created, new DateTime(2026, 1, 1)),
            Mv(_itemA, MovementType.Created, new DateTime(2026, 6, 1)),
            Mv(_itemA, MovementType.Created, new DateTime(2026, 12, 1)),
        ]);

        var result = await handler.Handle(
            new GetMovementsQuery(
                DateFrom: new DateTime(2026, 3, 1), DateTo: new DateTime(2026, 9, 1)),
            CancellationToken.None);

        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].OccurredAt.Should().Be(new DateTime(2026, 6, 1));
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);

        var handler = new GetMovementsQueryHandler(_currentUser, _context, _identityService);

        var result = await handler.Handle(new GetMovementsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
