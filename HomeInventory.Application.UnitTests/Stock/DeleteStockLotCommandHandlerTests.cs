using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.DeleteStockLot;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class DeleteStockLotCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _lotId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private DeleteStockLotCommandHandler BuildHandler(List<StockLot> stockLots)
    {
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        var movementsDbSet = new List<Movement>().BuildMockDbSet();
        _context.StockLots.Returns(stockLotsDbSet);
        _context.Movements.Returns(movementsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);
        _currentUser.UserId.Returns(_userId);

        return new DeleteStockLotCommandHandler(_currentUser, _context, new StockService(_context, _currentUser));
    }

    [Fact]
    public async Task Deletes_an_existing_lot_and_records_a_discard()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var lot = new StockLot
        {
            Id = _lotId, HouseholdId = _householdId, ItemId = itemId, LocationId = locationId, Quantity = 4,
        };
        var handler = BuildHandler([lot]);

        var result = await handler.Handle(new DeleteStockLotCommand(_lotId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.StockLots.Received(1).Remove(lot);
        // Retrofit: deleting a whole lot records a Discarded movement for the remaining quantity.
        _context.Movements.Received(1).Add(Arg.Is<Movement>(m =>
            m.Type == MovementType.Discarded
            && m.Quantity == 4
            && m.ItemId == itemId
            && m.FromLocationId == locationId
            && m.ToLocationId == null
            && m.PerformedByUserId == _userId));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_lot_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([]);

        var result = await handler.Handle(new DeleteStockLotCommand(_lotId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockErrors.LotNotFound);
        _context.StockLots.DidNotReceive().Remove(Arg.Any<StockLot>());
    }
}
