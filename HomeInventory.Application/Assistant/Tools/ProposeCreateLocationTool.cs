using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Proposal tool: registers a <see cref="ProposedActionType.CreateLocation"/> action.
/// Resolves the parent by name (if supplied), detects ambiguity and duplicate names,
/// and never mutates the database itself.
/// </summary>
public sealed class ProposeCreateLocationTool : IAssistantTool
{
    private readonly ISender _sender;
    private readonly IProposedActionsCollector _collector;

    public ProposeCreateLocationTool(ISender sender, IProposedActionsCollector collector)
    {
        _sender = sender;
        _collector = collector;
    }

    public string Name => "propose_create_location";

    public string Description =>
        "Propose creating a new storage location. Resolves the parent location by name, detects "
        + "ambiguity and duplicate names. The user must confirm before anything is created.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "The new location's name." },
            type = new
            {
                type = "string",
                @enum = new[] { "Zone", "Room", "Furniture", "Container", "Spot" },
                description = "Hierarchical level (Zone > Room > Furniture > Container > Spot).",
            },
            parentName = new { type = "string", description = "Name of the parent location (optional)." },
            parentId = new { type = "string", description = "GUID of the parent location when already known (optional)." },
        },
        required = new[] { "name", "type" },
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

        var typeName = AssistantToolJson.GetString(arguments, "type") ?? "Room";
        var parentName = AssistantToolJson.GetString(arguments, "parentName");
        var parentId = AssistantToolJson.GetGuid(arguments, "parentId");

        // Load the location tree for resolution and duplicate detection.
        var treeResult = await _sender.Send(new GetLocationTreeQuery(), cancellationToken);
        if (treeResult.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = treeResult.Error.Message }));
        }

        var allLocations = FlattenTree(treeResult.Value).ToList();

        // Duplicate name check (warn but do not block).
        var hasDuplicate = allLocations.Any(n =>
            string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));

        // Resolve parent.
        Guid? resolvedParentId = parentId;
        string? resolvedParentName = null;
        IReadOnlyList<MissingEntity> missingEntities = [];

        if (resolvedParentId is null && parentName is not null)
        {
            var matches = allLocations
                .Where(n => string.Equals(n.Name, parentName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                // Parent doesn't exist — mark it as a prerequisite.
                missingEntities = [new MissingEntity("location", parentName)];
            }
            else if (matches.Count == 1)
            {
                resolvedParentId = matches[0].Id;
                resolvedParentName = matches[0].Name;
            }
            else
            {
                // Ambiguous parent: ask the user to clarify.
                _collector.SetClarificationQuestion(new ClarificationQuestion(
                    $"Found multiple locations named '{parentName}'. Which one should be the parent?",
                    matches.Select(m => $"{m.Name} (id: {m.Id})").ToList()));

                return AssistantToolResult.FromContent(AssistantToolJson.Serialize(new
                {
                    disambiguation_needed = true,
                    message = $"Multiple locations named '{parentName}' found. Cannot propose until the user picks one.",
                    options = matches.Select(m => new { m.Id, m.Name }),
                }));
            }
        }

        var summary = BuildSummary(name, typeName, resolvedParentName, parentName, missingEntities, hasDuplicate);

        var action = new ProposedAction(
            Type: ProposedActionType.CreateLocation,
            MissingEntities: missingEntities,
            Summary: summary,
            HasDuplicateWarning: hasDuplicate,
            LocationName: name,
            LocationTypeName: typeName,
            ParentLocationId: resolvedParentId,
            ParentLocationName: resolvedParentName ?? (missingEntities.Count > 0 ? parentName : null));

        _collector.Add(action);

        return AssistantToolResult.FromContent(AssistantToolJson.Serialize(new
        {
            proposed = true,
            summary,
            duplicateWarning = hasDuplicate
                ? $"A location named '{name}' already exists. Warn the user."
                : null,
            message = "Proposed successfully. Summarise to the user and ask for confirmation.",
        }));
    }

    private static string BuildSummary(
        string name,
        string typeName,
        string? resolvedParentName,
        string? requestedParentName,
        IReadOnlyList<MissingEntity> missingEntities,
        bool hasDuplicate)
    {
        var desc = $"Create location '{name}' (type: {typeName})";
        if (resolvedParentName is not null) desc += $" under '{resolvedParentName}'";
        else if (requestedParentName is not null) desc += $" under '{requestedParentName}'";

        var parts = new List<string> { desc };
        if (missingEntities.Count > 0)
            parts.Add($"parent '{missingEntities[0].Name}' will also be created");
        if (hasDuplicate)
            parts.Add($"WARNING: a location named '{name}' already exists");

        return string.Join(" — ", parts);
    }

    internal static IEnumerable<LocationTreeNodeDto> FlattenTree(
        IEnumerable<LocationTreeNodeDto> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in FlattenTree(node.Children))
                yield return child;
        }
    }
}
