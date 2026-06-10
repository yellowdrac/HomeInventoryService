using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Commands.CreateItem;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class CreateItemCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private CreateItemCommandHandler BuildHandler(List<Item> items)
    {
        var itemsDbSet = items.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new CreateItemCommandHandler(_currentUser, _context);
    }

    [Fact]
    public async Task Normalizes_the_name_lowercasing_and_stripping_accents()
    {
        var handler = BuildHandler([]);

        var result = await handler.Handle(
            new CreateItemCommand("Pilas AA Recargables Ácido", "Electrónica", null, TrackingType.Quantity, "unit", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _context.Items.Received(1).Add(Arg.Is<Item>(i =>
            i.HouseholdId == _householdId
            && i.Name == "Pilas AA Recargables Ácido"
            && i.NormalizedName == "pilas aa recargables acido"));
    }

    [Fact]
    public async Task Creates_an_item_with_zero_total_quantity()
    {
        var handler = BuildHandler([]);

        var result = await handler.Handle(
            new CreateItemCommand("Towel", null, null, TrackingType.Unique, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuantity.Should().Be(0);
        result.Value.TrackingType.Should().Be(TrackingType.Unique);
    }

    [Fact]
    public async Task Rejects_a_duplicate_normalized_name_within_the_household()
    {
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdId,
            Name = "Cafe",
            NormalizedName = "cafe",
            TrackingType = TrackingType.Quantity,
        };
        var handler = BuildHandler([existing]);

        // "Café" normalizes to "cafe", colliding with the existing item.
        var result = await handler.Handle(
            new CreateItemCommand("Café", null, null, TrackingType.Quantity, "g", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.DuplicateName);
        _context.Items.DidNotReceive().Add(Arg.Any<Item>());
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var handler = BuildHandler([]);
        _currentUser.HouseholdId.Returns((Guid?)null);

        var result = await handler.Handle(
            new CreateItemCommand("Towel", null, null, TrackingType.Unique, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
