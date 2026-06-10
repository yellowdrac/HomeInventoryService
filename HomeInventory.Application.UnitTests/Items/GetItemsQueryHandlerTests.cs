using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Items.Queries.GetItems;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class GetItemsQueryHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private GetItemsQueryHandler BuildHandler(List<Item> items, List<StockLot> stockLots)
    {
        var itemsDbSet = items.BuildMockDbSet();
        var stockLotsDbSet = stockLots.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.StockLots.Returns(stockLotsDbSet);
        _currentUser.HouseholdId.Returns(_householdId);

        return new GetItemsQueryHandler(_currentUser, _context);
    }

    private Item Item(Guid id, string name, string normalized, string? category = null) => new()
    {
        Id = id,
        HouseholdId = _householdId,
        Name = name,
        NormalizedName = normalized,
        Category = category,
        TrackingType = TrackingType.Quantity,
    };

    [Fact]
    public async Task Filters_by_name_insensitive_to_accents_and_case()
    {
        var cafeId = Guid.NewGuid();
        var handler = BuildHandler(
            [
                Item(cafeId, "Café Molido", "cafe molido"),
                Item(Guid.NewGuid(), "Batteries", "batteries"),
            ],
            []);

        // Query "CAFÉ" must match the normalized "cafe molido".
        var result = await handler.Handle(new GetItemsQuery(NameFilter: "CAFÉ"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.Id == cafeId);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Sums_total_quantity_across_the_item_lots()
    {
        var itemId = Guid.NewGuid();
        var handler = BuildHandler(
            [Item(itemId, "Batteries", "batteries")],
            [
                new StockLot { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = Guid.NewGuid(), Quantity = 4 },
                new StockLot { Id = Guid.NewGuid(), HouseholdId = _householdId, ItemId = itemId, LocationId = Guid.NewGuid(), Quantity = 6 },
            ]);

        var result = await handler.Handle(new GetItemsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single(i => i.Id == itemId).TotalQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Filters_by_category()
    {
        var toolId = Guid.NewGuid();
        var handler = BuildHandler(
            [
                Item(toolId, "Hammer", "hammer", category: "Tools"),
                Item(Guid.NewGuid(), "Batteries", "batteries", category: "Electronics"),
            ],
            []);

        var result = await handler.Handle(new GetItemsQuery(Category: "Tools"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.Id == toolId);
    }
}
