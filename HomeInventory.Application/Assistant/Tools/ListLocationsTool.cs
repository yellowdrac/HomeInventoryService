using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using MediatR;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Tool: list the household's location tree (every root with its nested children). Wraps
/// <see cref="GetLocationTreeQuery"/>.
/// </summary>
public sealed class ListLocationsTool : IAssistantTool
{
    private readonly ISender _sender;

    public ListLocationsTool(ISender sender) => _sender = sender;

    public string Name => "list_locations";

    public string Description =>
        "List all storage locations in the household as a tree (zones, rooms, furniture, containers, "
        + "spots) with their nesting. Use this to answer 'what locations/places do I have?'.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new { },
    };

    public async Task<AssistantToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLocationTreeQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = result.Error.Message }));
        }

        var references = new List<AssistantReference>();
        var roots = result.Value.Select(node => Map(node, parentBreadcrumb: null, references)).ToList();

        return new AssistantToolResult(
            AssistantToolJson.Serialize(new { count = references.Count, locations = roots }),
            references);
    }

    private static object Map(
        LocationTreeNodeDto node,
        string? parentBreadcrumb,
        List<AssistantReference> references)
    {
        var breadcrumb = string.IsNullOrEmpty(parentBreadcrumb)
            ? node.Name
            : $"{parentBreadcrumb} > {node.Name}";

        references.Add(new AssistantReference(
            AssistantReferenceKind.Location, node.Id, node.Name, breadcrumb));

        return new
        {
            locationId = node.Id,
            name = node.Name,
            type = node.Type,
            children = node.Children.Select(child => Map(child, breadcrumb, references)),
        };
    }
}
