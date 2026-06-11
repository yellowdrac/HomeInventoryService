using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;
using HomeInventory.Application.Movements.Common;
using HomeInventory.Application.Movements.Queries.GetMovements;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MediatR;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _otherHouseholdId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly DateOnly _today = new(2026, 6, 10);
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private GetDashboardSummaryQueryHandler BuildHandler(
        List<Item> items,
        List<Location> locations,
        List<StockLot> stockLots,
        List<MovementDto>? recentMovements = null,
        bool movementsFail = false)
    {
        var movementsResult = movementsFail
            ? Result.Failure<PagedResult<MovementDto>>(HouseholdErrors.NoHousehold)
            : Result.Success(new PagedResult<MovementDto>(
                recentMovements ?? [], 1, 5, recentMovements?.Count ?? 0));
        _sender.Send(
                Arg.Any<IRequest<Result<PagedResult<MovementDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(movementsResult);

        var itemsDbSet = items.BuildMockDbSet();
        var locationsDbSet = locations.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.Locations.Returns(locationsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetDashboardSummaryQueryHandler(_currentUser, _context, _sender);
    }

    private Item Item(Guid? householdId = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = householdId ?? _householdId,
        Name = "Thing",
        NormalizedName = "thing",
    };

    private Location Loc(Guid? householdId = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = householdId ?? _householdId,
        Name = "Shelf",
        Type = LocationType.Container,
        QrSlug = Guid.NewGuid().ToString("N"),
    };

    private StockLot Lot(decimal quantity, DateOnly? expiration, Guid? householdId = null) => new()
    {
        Id = Guid.NewGuid(),
        HouseholdId = householdId ?? _householdId,
        ItemId = Guid.NewGuid(),
        LocationId = _locationId,
        Quantity = quantity,
        ExpirationDate = expiration,
    };

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new GetDashboardSummaryQueryHandler(_currentUser, _context, _sender);

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(AsOfDate: _today), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }

    [Fact]
    public async Task Computes_counts_scoped_to_the_household()
    {
        var items = new List<Item> { Item(), Item(), Item(_otherHouseholdId) };
        var locations = new List<Location> { Loc(), Loc(_otherHouseholdId) };
        var lots = new List<StockLot>
        {
            Lot(3, _today.AddDays(-2)),               // expired
            Lot(2, _today.AddDays(3)),                // expiring soon
            Lot(5, _today.AddDays(30)),               // ok, perishable
            Lot(4, null),                             // not perishable
            Lot(99, _today.AddDays(-1), _otherHouseholdId), // other household, ignored
        };
        var handler = BuildHandler(items, locations, lots);

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(WithinDays: 7, AsOfDate: _today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().Be(2);
        result.Value.TotalLocations.Should().Be(1);
        result.Value.TotalStockUnits.Should().Be(14); // 3 + 2 + 5 + 4, other household excluded
        result.Value.ExpiredCount.Should().Be(1);
        result.Value.ExpiringSoonCount.Should().Be(1);
    }

    [Fact]
    public async Task Returns_recent_movements_from_the_movements_query()
    {
        var newer = new MovementDto(
            Guid.NewGuid(), Guid.NewGuid(), "Bread", null, null, _locationId, "Shelf",
            1, MovementType.Created, null, Guid.NewGuid(), "Alice", new DateTime(2026, 6, 1));
        var older = new MovementDto(
            Guid.NewGuid(), Guid.NewGuid(), "Milk", _locationId, "Shelf", null, null,
            1, MovementType.Consumed, null, Guid.NewGuid(), "Alice", new DateTime(2026, 1, 1));
        var handler = BuildHandler([], [], [], recentMovements: [newer, older]);

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(RecentMovementsCount: 5, AsOfDate: _today),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RecentMovements.Should().HaveCount(2);
        result.Value.RecentMovements[0].Id.Should().Be(newer.Id);
        result.Value.RecentMovements[1].Id.Should().Be(older.Id);

        await _sender.Received(1).Send(
            Arg.Is<IRequest<Result<PagedResult<MovementDto>>>>(q =>
                q is GetMovementsQuery && ((GetMovementsQuery)q).Page == 1
                    && ((GetMovementsQuery)q).PageSize == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_a_failure_from_the_movements_query()
    {
        var handler = BuildHandler([], [], [], movementsFail: true);

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(AsOfDate: _today), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
