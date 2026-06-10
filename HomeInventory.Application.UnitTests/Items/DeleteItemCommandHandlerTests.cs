using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Commands.DeleteItem;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class DeleteItemCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private DeleteItemCommandHandler BuildHandler(List<Item> items, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new DeleteItemCommandHandler(_currentUser, _context);
    }

    private Item Target() => new()
    {
        Id = _itemId,
        HouseholdId = _householdId,
        Name = "Batteries",
        NormalizedName = "batteries",
        TrackingType = TrackingType.Quantity,
    };

    [Fact]
    public async Task Deletes_an_item_without_stock()
    {
        var item = Target();
        var handler = BuildHandler([item], []);

        var result = await handler.Handle(new DeleteItemCommand(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.Items.Received(1).Remove(item);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_deletion_when_the_item_still_has_stock()
    {
        var lot = new StockLot
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            ItemId = _itemId,
            LocationId = Guid.NewGuid(),
            Quantity = 5,
        };
        var handler = BuildHandler([Target()], [lot]);

        var result = await handler.Handle(new DeleteItemCommand(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.HasStock);
        _context.Items.DidNotReceive().Remove(Arg.Any<Item>());
    }

    [Fact]
    public async Task Fails_when_the_item_does_not_exist_in_the_household()
    {
        var handler = BuildHandler([], []);

        var result = await handler.Handle(new DeleteItemCommand(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.NotFound);
    }
}
