using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: find items by name or barcode and report where each is stored. Wraps
/// <see cref="SearchInventoryQuery"/>.
/// </summary>
public sealed class SearchInventoryTool : IAssistantTool
{
    private readonly ISender _sender;

    public SearchInventoryTool(ISender sender) => _sender = sender;

    public string Name => "search_inventory";

    public string Description =>
        "Search the household inventory for items by name (typo tolerant) or barcode. Returns the "
        + "matching items with their total quantity and every location where each is stored "
        + "(including the full location breadcrumb). Use this to answer 'where is my X?' questions.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "The item name or barcode to search for, e.g. 'batteries' or 'pilas'.",
            },
        },
        required = new[] { "query" },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var query = AssistantToolJson.GetString(arguments, "query");
        if (query is null)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "The 'query' argument is required." }));
        }

        var result = await _sender.Send(new SearchInventoryQuery(query), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = result.Error.Message }));
        }

        var items = result.Value.Items;
        var references = new List<AssistantReference>();

        var content = items.Select(item =>
        {
            references.Add(new AssistantReference(AssistantReferenceKind.Item, item.ItemId, item.Name));

            return new
            {
                itemId = item.ItemId,
                name = item.Name,
                category = item.Category,
                unit = item.Unit,
                totalQuantity = item.TotalQuantity,
                placements = item.Placements.Select(p =>
                {
                    references.Add(new AssistantReference(
                        AssistantReferenceKind.Location,
                        p.LocationId,
                        p.LocationName,
                        BuildBreadcrumb(p.Breadcrumb)));

                    return new
                    {
                        locationId = p.LocationId,
                        location = p.LocationName,
                        breadcrumb = BuildBreadcrumb(p.Breadcrumb),
                        quantity = p.Quantity,
                        expirationDate = p.ExpirationDate,
                    };
                }),
            };
        }).ToList();

        return new AssistantToolResult(
            AssistantToolJson.Serialize(new { count = content.Count, items = content }),
            references);
    }

    internal static string BuildBreadcrumb(IEnumerable<Locations.Common.LocationDto> breadcrumb) =>
        string.Join(" > ", breadcrumb.Select(b => b.Name));
}
