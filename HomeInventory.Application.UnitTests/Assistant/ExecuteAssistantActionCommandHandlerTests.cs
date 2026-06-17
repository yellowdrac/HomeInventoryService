using FluentAssertions;
using HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Commands.CreateItem;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Locations.Commands.CreateLocation;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Application.Stock.Commands.AddStock;
using HomeInventory.Application.Stock.Commands.MoveStock;
using HomeInventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class ExecuteAssistantActionCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();

    private ExecuteAssistantActionCommandHandler BuildHandler()
    {
        _currentUser.UserId.Returns(_userId);
        _currentUser.HouseholdId.Returns(_householdId);
        return new ExecuteAssistantActionCommandHandler(_currentUser, _sender, _context);
    }

    private static LocationDto MakeLocationDto(Guid id, string name) =>
        new(id, name, LocationType.Room, null, "slug");

    private static ItemDto MakeItemDto(Guid id, string name) =>
        new(id, name, null, null, TrackingType.Quantity, null, null, 0, null);

    [Fact]
    public async Task Fails_when_user_has_no_household()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new ExecuteAssistantActionCommandHandler(_currentUser, _sender, _context);

        var action = new ProposedAction(
            Type: ProposedActionType.CreateLocation,
            MissingEntities: [],
            Summary: "test",
            LocationName: "X",
            LocationTypeName: "Room");

        var result = await handler.Handle(
            new ExecuteAssistantActionCommand([action]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
        await _sender.DidNotReceiveWithAnyArgs().Send(default!, default);
    }

    [Fact]
    public async Task Executes_CreateLocation_and_returns_the_created_entity()
    {
        var locationId = Guid.NewGuid();
        _sender.Send(Arg.Any<CreateLocationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(MakeLocationDto(locationId, "Habitación de Diego")));

        var action = new ProposedAction(
            Type: ProposedActionType.CreateLocation,
            MissingEntities: [],
            Summary: "Create location",
            LocationName: "Habitación de Diego",
            LocationTypeName: "Room");

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand([action]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedEntities.Should().ContainSingle(e =>
            e.Kind == AssistantReferenceKind.Location
            && e.Id == locationId
            && e.Name == "Habitación de Diego");
    }

    [Fact]
    public async Task Executes_CreateItem_and_returns_the_created_entity()
    {
        var itemId = Guid.NewGuid();
        _sender.Send(Arg.Any<CreateItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(MakeItemDto(itemId, "Pilas AA")));

        var action = new ProposedAction(
            Type: ProposedActionType.CreateItem,
            MissingEntities: [],
            Summary: "Create item",
            ItemName: "Pilas AA",
            ItemCategory: "Hogar",
            ItemTrackingTypeName: "Quantity");

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand([action]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedEntities.Should().ContainSingle(e =>
            e.Kind == AssistantReferenceKind.Item
            && e.Id == itemId
            && e.Name == "Pilas AA");
    }

    [Fact]
    public async Task Executes_multi_step_CreateLocation_then_CreateItem_then_AddStock_in_order()
    {
        var locationId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _sender.Send(Arg.Any<CreateLocationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(MakeLocationDto(locationId, "Habitación de Diego")));

        _sender.Send(Arg.Any<CreateItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(MakeItemDto(itemId, "Pilas AA")));

        var fakeStockLot = new StockLotDto(Guid.NewGuid(), itemId, "Pilas AA", locationId, "Habitación de Diego", [], 2, null, null);
        _sender.Send(Arg.Any<AddStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(fakeStockLot));

        var actions = new List<ProposedAction>
        {
            new(Type: ProposedActionType.CreateLocation,
                MissingEntities: [],
                Summary: "Create location",
                LocationName: "Habitación de Diego",
                LocationTypeName: "Room"),
            new(Type: ProposedActionType.CreateItem,
                MissingEntities: [],
                Summary: "Create item",
                ItemName: "Pilas AA",
                ItemTrackingTypeName: "Quantity"),
            new(Type: ProposedActionType.AddStock,
                MissingEntities: [new("item", "Pilas AA"), new("location", "Habitación de Diego")],
                Summary: "Add stock",
                UnresolvedItemName: "Pilas AA",
                UnresolvedLocationName: "Habitación de Diego",
                Quantity: 2),
        };

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand(actions),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedEntities.Should().HaveCount(2);
        result.Value.CreatedEntities.Should().Contain(e => e.Kind == AssistantReferenceKind.Location);
        result.Value.CreatedEntities.Should().Contain(e => e.Kind == AssistantReferenceKind.Item);

        // Commands were dispatched in order.
        Received.InOrder(() =>
        {
            _sender.Send(Arg.Any<CreateLocationCommand>(), Arg.Any<CancellationToken>());
            _sender.Send(Arg.Any<CreateItemCommand>(), Arg.Any<CancellationToken>());
            _sender.Send(Arg.Any<AddStockCommand>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Rejects_AddStock_when_unresolved_item_was_not_created_in_a_preceding_step()
    {
        var action = new ProposedAction(
            Type: ProposedActionType.AddStock,
            MissingEntities: [],
            Summary: "Add stock",
            UnresolvedItemName: "Ghost Item",
            ResolvedLocationId: Guid.NewGuid(),
            Quantity: 1);

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand([action]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AssistantErrors.InvalidAction);
    }

    [Fact]
    public async Task Rejects_CreateItem_when_ItemName_is_missing()
    {
        var action = new ProposedAction(
            Type: ProposedActionType.CreateItem,
            MissingEntities: [],
            Summary: "bad action");

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand([action]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AssistantErrors.InvalidAction);
    }

    [Fact]
    public async Task Propagates_inner_command_failures_and_stops_execution()
    {
        _sender.Send(Arg.Any<CreateLocationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<LocationDto>(LocationErrors.ParentNotFound));

        var actions = new List<ProposedAction>
        {
            new(Type: ProposedActionType.CreateLocation,
                MissingEntities: [],
                Summary: "fail",
                LocationName: "X",
                LocationTypeName: "Room"),
            new(Type: ProposedActionType.CreateItem,
                MissingEntities: [],
                Summary: "should not run",
                ItemName: "Y",
                ItemTrackingTypeName: "Quantity"),
        };

        var result = await BuildHandler().Handle(
            new ExecuteAssistantActionCommand(actions),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LocationErrors.ParentNotFound);
        await _sender.DidNotReceive().Send(Arg.Any<CreateItemCommand>(), Arg.Any<CancellationToken>());
    }
}
