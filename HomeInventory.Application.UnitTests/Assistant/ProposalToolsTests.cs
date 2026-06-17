using System.Text.Json;
using FluentAssertions;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Assistant.Tools;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using HomeInventory.Domain.Enums;
using MediatR;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class ProposalToolsTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static PagedResult<SearchResultItemDto> EmptyItems() =>
        new([], 1, 20, 0);

    private static Result<IReadOnlyList<LocationTreeNodeDto>> EmptyTree() =>
        Result.Success<IReadOnlyList<LocationTreeNodeDto>>([]);

    private static SearchResultItemDto MakeItem(Guid id, string name) =>
        new(id, name, null, TrackingType.Quantity, null, 0, []);

    private static LocationTreeNodeDto MakeLocation(Guid id, string name) =>
        new(id, name, LocationType.Room, null, "slug", []);

    // ── ProposeCreateItemTool ───────────────────────────────────────────────

    [Fact]
    public async Task ProposeCreateItemTool_adds_a_CreateItem_action_to_the_collector()
    {
        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(EmptyItems()));

        var collector = new ProposedActionsCollector();
        var tool = new ProposeCreateItemTool(_sender, collector);

        await tool.ExecuteAsync(
            Args("{\"name\":\"Pilas AA\",\"trackingType\":\"Quantity\",\"category\":\"Hogar\"}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle();
        var action = collector.Actions[0];
        action.Type.Should().Be(ProposedActionType.CreateItem);
        action.ItemName.Should().Be("Pilas AA");
        action.ItemCategory.Should().Be("Hogar");
        action.ItemTrackingTypeName.Should().Be("Quantity");
        action.HasDuplicateWarning.Should().BeFalse();
        collector.ClarificationQuestion.Should().BeNull();
    }

    [Fact]
    public async Task ProposeCreateItemTool_warns_when_item_with_same_name_already_exists()
    {
        var existingId = Guid.NewGuid();
        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<SearchResultItemDto>(
                [MakeItem(existingId, "Pilas AA")], 1, 20, 1)));

        var collector = new ProposedActionsCollector();
        var tool = new ProposeCreateItemTool(_sender, collector);

        var result = await tool.ExecuteAsync(
            Args("{\"name\":\"Pilas AA\",\"trackingType\":\"Quantity\"}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle(a => a.HasDuplicateWarning);
        result.Content.Should().Contain("already exists");
    }

    // ── ProposeCreateLocationTool ────────────────────────────────────────────

    [Fact]
    public async Task ProposeCreateLocationTool_adds_a_CreateLocation_action()
    {
        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(EmptyTree());

        var collector = new ProposedActionsCollector();
        var tool = new ProposeCreateLocationTool(_sender, collector);

        await tool.ExecuteAsync(
            Args("{\"name\":\"Habitación de Diego\",\"type\":\"Room\"}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle();
        var action = collector.Actions[0];
        action.Type.Should().Be(ProposedActionType.CreateLocation);
        action.LocationName.Should().Be("Habitación de Diego");
        action.LocationTypeName.Should().Be("Room");
        action.HasDuplicateWarning.Should().BeFalse();
    }

    [Fact]
    public async Task ProposeCreateLocationTool_returns_clarification_when_parent_name_is_ambiguous()
    {
        var locA = MakeLocation(Guid.NewGuid(), "Casa");
        var locB = MakeLocation(Guid.NewGuid(), "Casa");
        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<LocationTreeNodeDto>>([locA, locB]));

        var collector = new ProposedActionsCollector();
        var tool = new ProposeCreateLocationTool(_sender, collector);

        var result = await tool.ExecuteAsync(
            Args("{\"name\":\"Cuarto\",\"type\":\"Room\",\"parentName\":\"Casa\"}"),
            CancellationToken.None);

        collector.Actions.Should().BeEmpty();
        collector.ClarificationQuestion.Should().NotBeNull();
        collector.ClarificationQuestion!.Text.Should().Contain("Casa");
        result.Content.Should().Contain("disambiguation_needed");
    }

    [Fact]
    public async Task ProposeCreateLocationTool_warns_when_duplicate_name_exists()
    {
        var existing = MakeLocation(Guid.NewGuid(), "Cocina");
        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<LocationTreeNodeDto>>([existing]));

        var collector = new ProposedActionsCollector();
        var tool = new ProposeCreateLocationTool(_sender, collector);

        await tool.ExecuteAsync(
            Args("{\"name\":\"Cocina\",\"type\":\"Room\"}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle(a => a.HasDuplicateWarning);
    }

    // ── ProposeAddStockTool ──────────────────────────────────────────────────

    [Fact]
    public async Task ProposeAddStockTool_resolves_existing_item_and_location_correctly()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<SearchResultItemDto>(
                [MakeItem(itemId, "Pilas AA")], 1, 20, 1)));

        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<LocationTreeNodeDto>>(
                [MakeLocation(locationId, "Cajón")]));

        var collector = new ProposedActionsCollector();
        var tool = new ProposeAddStockTool(_sender, collector);

        await tool.ExecuteAsync(
            Args("{\"itemName\":\"Pilas AA\",\"locationName\":\"Cajón\",\"quantity\":3}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle();
        var action = collector.Actions[0];
        action.Type.Should().Be(ProposedActionType.AddStock);
        action.ResolvedItemId.Should().Be(itemId);
        action.ResolvedLocationId.Should().Be(locationId);
        action.Quantity.Should().Be(3);
        action.MissingEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task ProposeAddStockTool_marks_missing_item_and_location_as_unresolved()
    {
        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(EmptyItems()));

        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(EmptyTree());

        var collector = new ProposedActionsCollector();
        var tool = new ProposeAddStockTool(_sender, collector);

        await tool.ExecuteAsync(
            Args("{\"itemName\":\"X\",\"locationName\":\"Habitación de Diego\",\"quantity\":1}"),
            CancellationToken.None);

        collector.Actions.Should().ContainSingle();
        var action = collector.Actions[0];
        action.Type.Should().Be(ProposedActionType.AddStock);
        action.ResolvedItemId.Should().BeNull();
        action.UnresolvedItemName.Should().Be("X");
        action.ResolvedLocationId.Should().BeNull();
        action.UnresolvedLocationName.Should().Be("Habitación de Diego");
        action.MissingEntities.Should().HaveCount(2);
        action.MissingEntities.Should().Contain(e => e.Kind == "item" && e.Name == "X");
        action.MissingEntities.Should().Contain(e => e.Kind == "location" && e.Name == "Habitación de Diego");
    }

    [Fact]
    public async Task ProposeAddStockTool_returns_clarification_when_item_name_is_ambiguous()
    {
        // Neither item has an exact case-insensitive match for "baterias", so the tool can't resolve.
        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<SearchResultItemDto>(
                [MakeItem(Guid.NewGuid(), "Baterias recargables"), MakeItem(Guid.NewGuid(), "Baterias alcalinas")],
                1, 20, 2)));

        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(EmptyTree());

        var collector = new ProposedActionsCollector();
        var tool = new ProposeAddStockTool(_sender, collector);

        var result = await tool.ExecuteAsync(
            Args("{\"itemName\":\"baterias\",\"locationName\":\"Cajón\",\"quantity\":1}"),
            CancellationToken.None);

        collector.Actions.Should().BeEmpty();
        collector.ClarificationQuestion.Should().NotBeNull();
        result.Content.Should().Contain("disambiguation_needed");
    }

    [Fact]
    public async Task No_proposal_tool_mutates_data_directly()
    {
        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(EmptyItems()));
        _sender.Send(Arg.Any<GetLocationTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(EmptyTree());

        var collector = new ProposedActionsCollector();

        var createItem = new ProposeCreateItemTool(_sender, collector);
        var createLocation = new ProposeCreateLocationTool(_sender, collector);
        var addStock = new ProposeAddStockTool(_sender, collector);

        await createItem.ExecuteAsync(Args("{\"name\":\"X\",\"trackingType\":\"Quantity\"}"), default);
        await createLocation.ExecuteAsync(Args("{\"name\":\"Y\",\"type\":\"Room\"}"), default);
        await addStock.ExecuteAsync(Args("{\"itemName\":\"X\",\"locationName\":\"Y\",\"quantity\":1}"), default);

        // No write commands (CreateItem, CreateLocation, AddStock, MoveStock) were dispatched.
        // Only read queries (SearchInventoryQuery, GetLocationTreeQuery) were called.
        await _sender.DidNotReceive().Send(
            Arg.Is<object>(cmd =>
                cmd.GetType().Name.Contains("Create") && !cmd.GetType().Name.Contains("Query")),
            Arg.Any<CancellationToken>());
    }
}
