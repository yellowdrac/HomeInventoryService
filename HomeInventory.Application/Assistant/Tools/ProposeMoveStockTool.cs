using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Proposal tool: registers a <see cref="ProposedActionType.MoveStock"/> action.
/// Resolves item, source location and destination location; the source must already exist
/// (can't move what isn't there), but the destination may be absent (will be created via a
/// preceding propose_create_location call). Never mutates data.
/// </summary>
public sealed class ProposeMoveStockTool : IAssistantTool
{
    private readonly ISender _sender;
    private readonly IProposedActionsCollector _collector;

    public ProposeMoveStockTool(ISender sender, IProposedActionsCollector collector)
    {
        _sender = sender;
        _collector = collector;
    }

    public string Name => "propose_move_stock";

    public string Description =>
        "Propose moving a quantity of an item from one location to another. Item and from-location "
        + "must exist. To-location may not exist yet (create it first with propose_create_location). "
        + "The user must confirm before the move is executed.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            itemName = new { type = "string", description = "Item name to search for." },
            itemId = new { type = "string", description = "Item GUID if already known." },
            fromLocationName = new { type = "string", description = "Source location name." },
            fromLocationId = new { type = "string", description = "Source location GUID if already known." },
            toLocationName = new { type = "string", description = "Destination location name." },
            toLocationId = new { type = "string", description = "Destination location GUID if already known." },
            quantity = new { type = "number", description = "Quantity to move (positive)." },
        },
        required = new[] { "quantity" },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var itemId = AssistantToolJson.GetGuid(arguments, "itemId");
        var itemName = AssistantToolJson.GetString(arguments, "itemName");
        var fromLocationId = AssistantToolJson.GetGuid(arguments, "fromLocationId");
        var fromLocationName = AssistantToolJson.GetString(arguments, "fromLocationName");
        var toLocationId = AssistantToolJson.GetGuid(arguments, "toLocationId");
        var toLocationName = AssistantToolJson.GetString(arguments, "toLocationName");
        var quantity = AssistantToolJson.GetDecimal(arguments, "quantity") ?? 1m;

        if (itemId is null && string.IsNullOrWhiteSpace(itemName))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'itemName' or 'itemId' is required." }));
        }

        if (fromLocationId is null && string.IsNullOrWhiteSpace(fromLocationName))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'fromLocationName' or 'fromLocationId' is required." }));
        }

        if (toLocationId is null && string.IsNullOrWhiteSpace(toLocationName))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'toLocationName' or 'toLocationId' is required." }));
        }

        // Load location tree once for all location resolutions.
        var treeResult = await _sender.Send(new GetLocationTreeQuery(), cancellationToken);
        if (treeResult.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = treeResult.Error.Message }));
        }

        var allLocations = ProposeCreateLocationTool.FlattenTree(treeResult.Value).ToList();

        // Resolve item (must exist — can't move what isn't there).
        Guid? resolvedItemId = itemId;
        string? resolvedItemDisplay = null;

        if (resolvedItemId is null && !string.IsNullOrEmpty(itemName))
        {
            if (itemName.Length < SearchInventoryQueryValidator.MinQueryLength)
            {
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { error = $"Item '{itemName}' not found." }));
            }

            var searchResult = await _sender.Send(new SearchInventoryQuery(itemName), cancellationToken);
            if (searchResult.IsFailure || searchResult.Value.Items.Count == 0)
            {
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { error = $"No item found matching '{itemName}'." }));
            }

            var items = searchResult.Value.Items;
            var exact = items.FirstOrDefault(i =>
                string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));

            if (exact is not null)
            {
                resolvedItemId = exact.ItemId;
                resolvedItemDisplay = exact.Name;
            }
            else if (items.Count == 1)
            {
                resolvedItemId = items[0].ItemId;
                resolvedItemDisplay = items[0].Name;
            }
            else
            {
                var q = new ClarificationQuestion(
                    $"Found multiple items matching '{itemName}'. Which one did you mean?",
                    items.Select(i => i.Name).ToList());
                _collector.SetClarificationQuestion(q);
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { disambiguation_needed = true, message = q.Text }));
            }
        }

        // Resolve from-location (must exist).
        Guid? resolvedFromId = fromLocationId;
        string? resolvedFromDisplay = null;

        if (resolvedFromId is null && !string.IsNullOrEmpty(fromLocationName))
        {
            var matches = allLocations
                .Where(n => string.Equals(n.Name, fromLocationName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { error = $"Source location '{fromLocationName}' not found." }));
            }

            if (matches.Count == 1)
            {
                resolvedFromId = matches[0].Id;
                resolvedFromDisplay = matches[0].Name;
            }
            else
            {
                var q = new ClarificationQuestion(
                    $"Found multiple source locations named '{fromLocationName}'. Which one?",
                    matches.Select(m => $"{m.Name} (id: {m.Id})").ToList());
                _collector.SetClarificationQuestion(q);
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { disambiguation_needed = true, message = q.Text }));
            }
        }

        // Resolve to-location (may be missing — will be created via a preceding proposal).
        Guid? resolvedToId = toLocationId;
        string? resolvedToDisplay = null;
        string? unresolvedToName = null;

        if (resolvedToId is null && !string.IsNullOrEmpty(toLocationName))
        {
            var matches = allLocations
                .Where(n => string.Equals(n.Name, toLocationName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                unresolvedToName = toLocationName;
            }
            else if (matches.Count == 1)
            {
                resolvedToId = matches[0].Id;
                resolvedToDisplay = matches[0].Name;
            }
            else
            {
                var q = new ClarificationQuestion(
                    $"Found multiple destination locations named '{toLocationName}'. Which one?",
                    matches.Select(m => $"{m.Name} (id: {m.Id})").ToList());
                _collector.SetClarificationQuestion(q);
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { disambiguation_needed = true, message = q.Text }));
            }
        }

        var missingEntities = unresolvedToName is not null
            ? new List<MissingEntity> { new("location", unresolvedToName) }
            : new List<MissingEntity>();

        var itemLabel = resolvedItemDisplay ?? itemName;
        var fromLabel = resolvedFromDisplay ?? fromLocationName;
        var toLabel = resolvedToDisplay ?? unresolvedToName ?? toLocationName;

        var summaryParts = new List<string>
            { $"Move {quantity} '{itemLabel}' from '{fromLabel}' to '{toLabel}'" };
        if (unresolvedToName is not null)
            summaryParts.Add($"will also create destination location '{unresolvedToName}'");
        var summary = string.Join(" — ", summaryParts);

        var action = new ProposedAction(
            Type: ProposedActionType.MoveStock,
            MissingEntities: missingEntities,
            Summary: summary,
            ResolvedItemId: resolvedItemId,
            ResolvedFromLocationId: resolvedFromId,
            UnresolvedFromLocationName: resolvedFromId is null ? fromLocationName : null,
            ResolvedToLocationId: resolvedToId,
            UnresolvedToLocationName: unresolvedToName,
            Quantity: quantity);

        _collector.Add(action);

        return AssistantToolResult.FromContent(AssistantToolJson.Serialize(new
        {
            proposed = true,
            summary,
            missingEntities = missingEntities.Select(e => new { e.Kind, e.Name }),
            message = "Proposed successfully. Summarise to the user and ask for confirmation.",
        }));
    }
}
