using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Expirations.Queries.GetExpiringStock;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: list perishable stock that is expiring soon or already expired (FEFO order). Wraps
/// <see cref="GetExpiringStockQuery"/>.
/// </summary>
public sealed class GetExpiringStockTool : IAssistantTool
{
    private const int DefaultWithinDays = 7;

    private readonly ISender _sender;

    public GetExpiringStockTool(ISender sender) => _sender = sender;

    public string Name => "get_expiring_stock";

    public string Description =>
        "List perishable stock lots that expire within a number of days (already-expired lots are "
        + "included), ordered earliest-expiry first. Use this to answer 'what is expiring soon / "
        + "what is about to go bad?'.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            withinDays = new
            {
                type = "integer",
                description = $"Look-ahead window in days. Defaults to {DefaultWithinDays}.",
            },
        },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var withinDays = AssistantToolJson.GetInt(arguments, "withinDays") ?? DefaultWithinDays;

        var result = await _sender.Send(
            new GetExpiringStockQuery(WithinDays: withinDays), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = result.Error.Message }));
        }

        var lots = result.Value;
        var references = new List<AssistantReference>();

        var content = lots.Select(lot =>
        {
            references.Add(new AssistantReference(AssistantReferenceKind.Item, lot.ItemId, lot.ItemName));
            references.Add(new AssistantReference(
                AssistantReferenceKind.Location,
                lot.LocationId,
                lot.LocationName,
                string.Join(" > ", lot.Breadcrumb.Select(b => b.Name))));

            return new
            {
                itemId = lot.ItemId,
                item = lot.ItemName,
                locationId = lot.LocationId,
                location = lot.LocationName,
                breadcrumb = string.Join(" > ", lot.Breadcrumb.Select(b => b.Name)),
                quantity = lot.Quantity,
                expirationDate = lot.ExpirationDate,
                daysUntilExpiry = lot.DaysUntilExpiry,
                status = lot.Status,
            };
        }).ToList();

        return new AssistantToolResult(
            AssistantToolJson.Serialize(new { withinDays, count = content.Count, lots = content }),
            references);
    }
}
