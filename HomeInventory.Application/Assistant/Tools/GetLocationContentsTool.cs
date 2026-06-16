using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Text;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using HomeInventory.Application.Stock.Queries.GetLocationContents;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: list what is stored in a given location. Accepts a location id or a location name (resolved
/// against the location tree). Wraps <see cref="GetLocationContentsQuery"/>.
/// </summary>
public sealed class GetLocationContentsTool : IAssistantTool
{
    private readonly ISender _sender;

    public GetLocationContentsTool(ISender sender) => _sender = sender;

    public string Name => "get_location_contents";

    public string Description =>
        "List the stock stored in a specific location (items, quantities and expiry dates). Provide "
        + "either 'locationId' or 'locationName'. Use this to answer 'what is in the X?'.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            locationId = new
            {
                type = "string",
                description = "The location's GUID, when known.",
            },
            locationName = new
            {
                type = "string",
                description = "The location's name, used when the id is unknown (e.g. 'pantry').",
            },
        },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var locationId = AssistantToolJson.GetGuid(arguments, "locationId");
        string? resolvedBreadcrumb = null;
        string? resolvedName = null;

        if (locationId is null)
        {
            var name = AssistantToolJson.GetString(arguments, "locationName");
            if (name is null)
            {
                return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                    new { error = "Provide either 'locationId' or 'locationName'." }));
            }

            var tree = await _sender.Send(new GetLocationTreeQuery(), cancellationToken);
            if (tree.IsFailure)
            {
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { error = tree.Error.Message }));
            }

            var flat = new List<(Guid Id, string Name, string Breadcrumb)>();
            Flatten(tree.Value, parentBreadcrumb: null, flat);

            var target = TextNormalization.Normalize(name);
            var match = flat.FirstOrDefault(l => TextNormalization.Normalize(l.Name) == target);
            if (match == default)
            {
                match = flat.FirstOrDefault(l => TextNormalization.Normalize(l.Name).Contains(target));
            }

            if (match == default)
            {
                return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                    new { found = false, message = $"No location matches '{name}'." }));
            }

            locationId = match.Id;
            resolvedName = match.Name;
            resolvedBreadcrumb = match.Breadcrumb;
        }

        var result = await _sender.Send(
            new GetLocationContentsQuery(locationId.Value), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(AssistantToolJson.Serialize(
                new { found = false, message = result.Error.Message }));
        }

        var lots = result.Value;
        var references = new List<AssistantReference>();

        if (lots.Count > 0)
        {
            resolvedName ??= lots[0].LocationName;
            resolvedBreadcrumb ??= string.Join(" > ", lots[0].LocationBreadcrumb);
        }

        references.Add(new AssistantReference(
            AssistantReferenceKind.Location, locationId.Value, resolvedName ?? string.Empty, resolvedBreadcrumb));

        var content = new
        {
            locationId = locationId.Value,
            location = resolvedName,
            breadcrumb = resolvedBreadcrumb,
            itemCount = lots.Count,
            contents = lots.Select(lot =>
            {
                references.Add(new AssistantReference(
                    AssistantReferenceKind.Item, lot.ItemId, lot.ItemName));

                return new
                {
                    itemId = lot.ItemId,
                    item = lot.ItemName,
                    quantity = lot.Quantity,
                    expirationDate = lot.ExpirationDate,
                };
            }),
        };

        return new AssistantToolResult(AssistantToolJson.Serialize(content), references);
    }

    private static void Flatten(
        IEnumerable<LocationTreeNodeDto> nodes,
        string? parentBreadcrumb,
        List<(Guid Id, string Name, string Breadcrumb)> sink)
    {
        foreach (var node in nodes)
        {
            var breadcrumb = string.IsNullOrEmpty(parentBreadcrumb)
                ? node.Name
                : $"{parentBreadcrumb} > {node.Name}";
            sink.Add((node.Id, node.Name, breadcrumb));
            Flatten(node.Children, breadcrumb, sink);
        }
    }
}
