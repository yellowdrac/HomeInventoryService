using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Services;
using HomeInventory.Application.Stock.Commands.DeleteStockLot;
using HomeInventory.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class DeleteStockLotCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _lotId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private DeleteStockLotCommandHandler BuildHandler(List<StockLot> stockLots)
    {
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.StockLots.Returns(stockLotsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new DeleteStockLotCommandHandler(_currentUser, _context, new StockService(_context));
    }

    [Fact]
    public async Task Deletes_an_existing_lot()
    {
        var lot = new StockLot
        {
            Id = _lotId, HouseholdId = _householdId, ItemId = Guid.NewGuid(), LocationId = Guid.NewGuid(), Quantity = 1,
        };
        var handler = BuildHandler([lot]);

        var result = await handler.Handle(new DeleteStockLotCommand(_lotId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.StockLots.Received(1).Remove(lot);
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
