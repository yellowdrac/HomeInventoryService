using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Proposal tool: registers a <see cref="ProposedActionType.AddStock"/> action.
/// Resolves the item and location by name or id; marks them as missing if not found
/// (a preceding propose_create_* call must supply the creation params in that case).
/// Never mutates data; execution requires user confirmation via <c>POST /api/assistant/execute</c>.
/// </summary>
public sealed class ProposeAddStockTool : IAssistantTool
{
    private readonly ISender _sender;
    private readonly IProposedActionsCollector _collector;

    public ProposeAddStockTool(ISender sender, IProposedActionsCollector collector)
    {
        _sender = sender;
        _collector = collector;
    }

    public string Name => "propose_add_stock";

    public string Description =>
        "Propose adding stock of an item at a location. Resolves item and location by name or id. "
        + "If either does not exist, mark it as missing and include a separate propose_create_* call "
        + "before this one so the execute step can create them first.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            itemName = new { type = "string", description = "Item name to search for." },
            itemId = new { type = "string", description = "Item GUID if already known." },
            locationName = new { type = "string", description = "Destination location name." },
            locationId = new { type = "string", description = "Destination location GUID if already known." },
            quantity = new { type = "number", description = "Quantity to add (positive)." },
            expirationDate = new { type = "string", description = "Expiration date as YYYY-MM-DD (optional)." },
        },
        required = new[] { "quantity" },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var itemId = AssistantToolJson.GetGuid(arguments, "itemId");
        var itemName = AssistantToolJson.GetString(arguments, "itemName");
        var locationId = AssistantToolJson.GetGuid(arguments, "locationId");
        var locationName = AssistantToolJson.GetString(arguments, "locationName");
        var quantity = AssistantToolJson.GetDecimal(arguments, "quantity") ?? 1m;
        var expirationDateStr = AssistantToolJson.GetString(arguments, "expirationDate");

        if (itemId is null && string.IsNullOrWhiteSpace(itemName))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'itemName' or 'itemId' is required." }));
        }

        if (locationId is null && string.IsNullOrWhiteSpace(locationName))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'locationName' or 'locationId' is required." }));
        }

        // Resolve item.
        Guid? resolvedItemId = itemId;
        string? resolvedItemDisplay = null;
        string? unresolvedItemName = null;

        if (resolvedItemId is null && !string.IsNullOrEmpty(itemName))
        {
            var (resolved, display, unresolved, clarification) =
                await ResolveItemAsync(itemName, cancellationToken);

            if (clarification is not null)
            {
                _collector.SetClarificationQuestion(clarification);
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { disambiguation_needed = true, message = clarification.Text }));
            }

            resolvedItemId = resolved;
            resolvedItemDisplay = display;
            unresolvedItemName = unresolved;
        }

        // Resolve location.
        Guid? resolvedLocationId = locationId;
        string? resolvedLocationDisplay = null;
        string? unresolvedLocationName = null;

        if (resolvedLocationId is null && !string.IsNullOrEmpty(locationName))
        {
            var (resolved, display, unresolved, clarification) =
                await ResolveLocationAsync(locationName, cancellationToken);

            if (clarification is not null)
            {
                _collector.SetClarificationQuestion(clarification);
                return AssistantToolResult.FromContent(
                    AssistantToolJson.Serialize(new { disambiguation_needed = true, message = clarification.Text }));
            }

            resolvedLocationId = resolved;
            resolvedLocationDisplay = display;
            unresolvedLocationName = unresolved;
        }

        var missingEntities = new List<MissingEntity>();
        if (unresolvedItemName is not null) missingEntities.Add(new MissingEntity("item", unresolvedItemName));
        if (unresolvedLocationName is not null) missingEntities.Add(new MissingEntity("location", unresolvedLocationName));

        var itemDisplay = resolvedItemDisplay ?? unresolvedItemName ?? itemName;
        var locationDisplay = resolvedLocationDisplay ?? unresolvedLocationName ?? locationName;

        var summaryParts = new List<string> { $"Add {quantity} '{itemDisplay}' to '{locationDisplay}'" };
        if (unresolvedItemName is not null) summaryParts.Add($"will also create item '{unresolvedItemName}'");
        if (unresolvedLocationName is not null) summaryParts.Add($"will also create location '{unresolvedLocationName}'");
        var summary = string.Join(" — ", summaryParts);

        DateOnly? expirationDate = null;
        if (!string.IsNullOrEmpty(expirationDateStr) &&
            DateOnly.TryParse(expirationDateStr, out var parsedDate))
        {
            expirationDate = parsedDate;
        }

        var action = new ProposedAction(
            Type: ProposedActionType.AddStock,
            MissingEntities: missingEntities,
            Summary: summary,
            ResolvedItemId: resolvedItemId,
            UnresolvedItemName: unresolvedItemName,
            ResolvedLocationId: resolvedLocationId,
            UnresolvedLocationName: unresolvedLocationName,
            Quantity: quantity,
            ExpirationDate: expirationDate);

        _collector.Add(action);

        return AssistantToolResult.FromContent(AssistantToolJson.Serialize(new
        {
            proposed = true,
            summary,
            missingEntities = missingEntities.Select(e => new { e.Kind, e.Name }),
            message = "Proposed successfully. Summarise to the user and ask for confirmation.",
        }));
    }

    private async Task<(Guid? ResolvedId, string? Display, string? Missing, ClarificationQuestion? Clarification)>
        ResolveItemAsync(string name, CancellationToken ct)
    {
        if (name.Length < SearchInventoryQueryValidator.MinQueryLength)
            return (null, null, name, null);

        var result = await _sender.Send(new SearchInventoryQuery(name), ct);
        if (result.IsFailure || result.Value.Items.Count == 0)
            return (null, null, name, null);

        var items = result.Value.Items;
        var exact = items.FirstOrDefault(i =>
            string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
            return (exact.ItemId, exact.Name, null, null);

        if (items.Count == 1)
            return (items[0].ItemId, items[0].Name, null, null);

        var question = new ClarificationQuestion(
            $"Found multiple items matching '{name}'. Which one did you mean?",
            items.Select(i => i.Name).ToList());

        return (null, null, null, question);
    }

    private async Task<(Guid? ResolvedId, string? Display, string? Missing, ClarificationQuestion? Clarification)>
        ResolveLocationAsync(string name, CancellationToken ct)
    {
        var result = await _sender.Send(new GetLocationTreeQuery(), ct);
        if (result.IsFailure)
            return (null, null, name, null);

        var matches = ProposeCreateLocationTool.FlattenTree(result.Value)
            .Where(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return (null, null, name, null);

        if (matches.Count == 1)
            return (matches[0].Id, matches[0].Name, null, null);

        var question = new ClarificationQuestion(
            $"Found multiple locations named '{name}'. Which one did you mean?",
            matches.Select(m => $"{m.Name} (id: {m.Id})").ToList());

        return (null, null, null, question);
    }
}
