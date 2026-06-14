using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Items.Queries.GetItemById;
using HomeInventory.Application.Items.Queries.SearchInventory;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: get the full detail of a single item (its fields plus every stock lot, with locations and
/// expiry dates). Accepts an item id or, failing that, resolves an item name through
/// <see cref="SearchInventoryQuery"/> before wrapping <see cref="GetItemByIdQuery"/>.
/// </summary>
public sealed class GetItemDetailsTool : IAssistantTool
{
    private readonly ISender _sender;

    public GetItemDetailsTool(ISender sender) => _sender = sender;

    public string Name => "get_item_details";

    public string Description =>
        "Get detailed information about one item: its category, unit, total quantity and every stock "
        + "lot (location, breadcrumb, quantity and expiration date). Provide either 'itemId' or "
        + "'itemName'.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            itemId = new
            {
                type = "string",
                description = "The item's GUID, when known.",
            },
            itemName = new
            {
                type = "string",
                description = "The item's name, used when the id is unknown (resolved by search).",
            },
        },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var itemId = AssistantToolJson.GetGuid(arguments, "itemId");

        if (itemId is null)
        {
            var name = AssistantToolJson.GetString(arguments, "itemName");
            if (name is null)
            {
                return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                    new { error = "Provide either 'itemId' or 'itemName'." }));
            }

            var search = await _sender.Send(new SearchInventoryQuery(name), cancellationToken);
            if (search.IsFailure)
            {
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { error = search.Error.Message }));
            }

            var best = search.Value.Items.FirstOrDefault();
            if (best is null)
            {
                return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                    new { found = false, message = $"No item matches '{name}'." }));
            }

            itemId = best.ItemId;
        }

        var result = await _sender.Send(new GetItemByIdQuery(itemId.Value), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                new { found = false, message = result.Error.Message }));
        }

        var item = result.Value;
        var references = new List<AssistantReference>
        {
            new(AssistantReferenceKind.Item, item.Id, item.Name),
        };

        var content = new
        {
            itemId = item.Id,
            name = item.Name,
            category = item.Category,
            barcode = item.Barcode,
            unit = item.Unit,
            totalQuantity = item.TotalQuantity,
            lots = item.Lots.Select(lot =>
            {
                references.Add(new AssistantReference(
                    AssistantReferenceKind.Location,
                    lot.LocationId,
                    lot.LocationName,
                    string.Join(" > ", lot.LocationBreadcrumb)));

                return new
                {
                    locationId = lot.LocationId,
                    location = lot.LocationName,
                    breadcrumb = string.Join(" > ", lot.LocationBreadcrumb),
                    quantity = lot.Quantity,
                    expirationDate = lot.ExpirationDate,
                    acquiredDate = lot.AcquiredDate,
                };
            }),
        };

        return new AssistantToolResult(AssistantToolJson.Serialize(content), references);
    }
}
