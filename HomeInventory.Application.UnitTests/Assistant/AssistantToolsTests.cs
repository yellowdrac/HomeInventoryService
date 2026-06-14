using System.Text.Json;
using FluentAssertions;
using HomeInventory.Application.Assistant.Tools;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Enums;
using MediatR;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class AssistantToolsTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task Search_tool_dispatches_the_household_scoped_query_and_returns_its_data()
    {
        var itemId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var placement = new SearchPlacementDto(
            locationId,
            "Drawer",
            [new LocationDto(locationId, "Drawer", LocationType.Container, null, "drawer")],
            3,
            null);
        var item = new SearchResultItemDto(
            itemId, "Batteries", "Tools", TrackingType.Quantity, "unit", 3, [placement]);
        var paged = new PagedResult<SearchResultItemDto>([item], 1, 20, 1);

        _sender.Send(Arg.Any<SearchInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(paged));

        var tool = new SearchInventoryTool(_sender);

        var result = await tool.ExecuteAsync(Args("{\"query\":\"batteries\"}"), CancellationToken.None);

        // The tool wraps the existing, household-scoped MediatR query (never the DbContext directly).
        await _sender.Received(1).Send(
            Arg.Is<SearchInventoryQuery>(q => q.Query == "batteries"),
            Arg.Any<CancellationToken>());

        result.Content.Should().Contain("Batteries").And.Contain("Drawer");
        result.References.Should().Contain(r => r.Id == itemId)
            .And.Contain(r => r.Id == locationId);
    }

    [Fact]
    public async Task Search_tool_rejects_a_missing_query_argument()
    {
        var tool = new SearchInventoryTool(_sender);

        var result = await tool.ExecuteAsync(Args("{}"), CancellationToken.None);

        result.Content.Should().Contain("required");
        await _sender.DidNotReceiveWithAnyArgs().Send(default!, default);
    }
}
