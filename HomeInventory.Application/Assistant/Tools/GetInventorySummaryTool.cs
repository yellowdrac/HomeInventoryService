using System.Text.Json;
using HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: high-level inventory counts (items, locations, total stock units, expired and
/// expiring-soon lots). Wraps <see cref="GetDashboardSummaryQuery"/>.
/// </summary>
public sealed class GetInventorySummaryTool : IAssistantTool
{
    private readonly ISender _sender;

    public GetInventorySummaryTool(ISender sender) => _sender = sender;

    public string Name => "get_inventory_summary";

    public string Description =>
        "Get overall inventory counts for the household: number of items, number of locations, total "
        + "stock units, and how many lots are expired or expiring soon. Use this for 'how much do I "
        + "have / give me an overview' questions.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new { },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = result.Error.Message }));
        }

        var summary = result.Value;
        var content = new
        {
            totalItems = summary.TotalItems,
            totalLocations = summary.TotalLocations,
            totalStockUnits = summary.TotalStockUnits,
            expiredCount = summary.ExpiredCount,
            expiringSoonCount = summary.ExpiringSoonCount,
        };

        return AssistantToolResult.FromContent(AssistantToolJson.Serialize(content));
    }
}
