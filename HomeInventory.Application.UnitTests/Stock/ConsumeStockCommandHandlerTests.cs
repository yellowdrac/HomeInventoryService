using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.ConsumeStock;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class ConsumeStockCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _lotId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private ConsumeStockCommandHandler BuildHandler(List<StockLot> stockLots)
    {
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        var movementsDbSet = new List<Movement>().BuildMockDbSet();
        _context.StockLots.Returns(stockLotsDbSet);
        _context.Movements.Returns(movementsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);
        _currentUser.UserId.Returns(_userId);

        return new ConsumeStockCommandHandler(_currentUser, _context, new StockService(_context, _currentUser));
    }

    private StockLot Lot(decimal quantity) => new()
    {
        Id = _lotId,
        HouseholdId = _householdId,
        ItemId = _itemId,
        LocationId = _locationId,
        Quantity = quantity,
    };

    [Fact]
    public async Task Reduces_the_lot_and_records_consumed()
    {
        var lot = Lot(10);
        var handler = BuildHandler([lot]);

        var result = await handler.Handle(
            new ConsumeStockCommand(_lotId, 4, "used"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lot.Quantity.Should().Be(6);
        _context.StockLots.DidNotReceive().Remove(Arg.Any<StockLot>());
        _context.Movements.Received(1).Add(Arg.Is<Movement>(m =>
            m.Type == MovementType.Consumed
            && m.Quantity == 4
            && m.FromLocationId == _locationId
            && m.ToLocationId == null
            && m.Reason == "used"
            && m.PerformedByUserId == _userId));
    }

    [Fact]
    public async Task Removes_the_lot_when_it_reaches_zero()
    {
        var lot = Lot(4);
        var handler = BuildHandler([lot]);

        var result = await handler.Handle(
            new ConsumeStockCommand(_lotId, 4, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.StockLots.Received(1).Remove(lot);
        _context.Movements.Received(1).Add(Arg.Is<Movement>(m => m.Type == MovementType.Consumed && m.Quantity == 4));
    }

    [Fact]
    public async Task Rejects_a_quantity_above_the_available()
    {
        var handler = BuildHandler([Lot(3)]);

        var result = await handler.Handle(
            new ConsumeStockCommand(_lotId, 4, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.InsufficientQuantity);
        _context.Movements.DidNotReceive().Add(Arg.Any<Movement>());
    }

    [Fact]
    public async Task Fails_when_the_lot_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([]);

        var result = await handler.Handle(
            new ConsumeStockCommand(_lotId, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.LotNotFound);
    }
}
