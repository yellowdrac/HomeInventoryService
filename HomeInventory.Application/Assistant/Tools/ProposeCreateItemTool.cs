using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Items.Queries.SearchInventory;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Proposal tool: registers a <see cref="ProposedActionType.CreateItem"/> action in the
/// <see cref="IProposedActionsCollector"/>. Checks for potential duplicates before proposing.
/// Does NOT create the item; execution happens only after user confirmation via
/// <c>POST /api/assistant/execute</c>.
/// </summary>
public sealed class ProposeCreateItemTool : IAssistantTool
{
    private readonly ISender _sender;
    private readonly IProposedActionsCollector _collector;

    public ProposeCreateItemTool(ISender sender, IProposedActionsCollector collector)
    {
        _sender = sender;
        _collector = collector;
    }

    public string Name => "propose_create_item";

    public string Description =>
        "Propose creating a new item in the household inventory. Checks for duplicates and warns "
        + "the user if a similarly-named item already exists. The user must confirm before the "
        + "item is actually created.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "The item name." },
            category = new { type = "string", description = "Optional category (e.g. Food, Cleaning, Hogar)." },
            trackingType = new
            {
                type = "string",
                @enum = new[] { "Quantity", "Unique" },
                description = "Quantity for consumables tracked by amount; Unique for single one-of-a-kind items.",
            },
            unit = new { type = "string", description = "Unit label for quantity-tracked items (e.g. kg, units, bottles)." },
        },
        required = new[] { "name", "trackingType" },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var name = AssistantToolJson.GetString(arguments, "name");
        if (name is null)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "'name' is required." }));
        }

        var category = AssistantToolJson.GetString(arguments, "category");
        var trackingTypeName = AssistantToolJson.GetString(arguments, "trackingType") ?? "Quantity";
        var unit = AssistantToolJson.GetString(arguments, "unit");

        // Check for an existing item with the same name (only when name is long enough to search).
        var hasDuplicate = false;
        string? duplicateName = null;
        if (name.Length >= SearchInventoryQueryValidator.MinQueryLength)
        {
            var searchResult = await _sender.Send(new SearchInventoryQuery(name), cancellationToken);
            if (searchResult.IsSuccess)
            {
                var exact = searchResult.Value.Items.FirstOrDefault(i =>
                    string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                {
                    hasDuplicate = true;
                    duplicateName = exact.Name;
                }
            }
        }

        var summary = BuildSummary(name, category, trackingTypeName, unit, hasDuplicate, duplicateName);

        var action = new ProposedAction(
            Type: ProposedActionType.CreateItem,
            MissingEntities: [],
            Summary: summary,
            HasDuplicateWarning: hasDuplicate,
            ItemName: name,
            ItemCategory: category,
            ItemTrackingTypeName: trackingTypeName,
            ItemUnit: unit);

        _collector.Add(action);

        return AssistantToolResult.FromContent(AssistantToolJson.Serialize(new
        {
            proposed = true,
            summary,
            duplicateWarning = hasDuplicate
                ? $"An item named '{duplicateName}' already exists. Tell the user and ask them to confirm they want a separate item."
                : null,
            message = "Proposed successfully. Summarise to the user and ask for confirmation.",
        }));
    }

    private static string BuildSummary(
        string name,
        string? category,
        string trackingTypeName,
        string? unit,
        bool hasDuplicate,
        string? duplicateName)
    {
        var parts = new List<string> { $"Create item '{name}'" };
        if (category is not null) parts[0] += $" (category: {category})";
        parts[0] += $", {trackingTypeName}-tracked";
        if (unit is not null) parts[0] += $", unit: {unit}";
        if (hasDuplicate) parts.Add($"WARNING: item '{duplicateName}' already exists");
        return string.Join(" — ", parts);
    }
}
