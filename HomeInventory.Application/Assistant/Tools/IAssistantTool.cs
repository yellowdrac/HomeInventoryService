using System.Text.Json;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// A read-only capability exposed to the LLM. Each tool wraps an existing inventory query (dispatched
/// via MediatR) and is therefore scoped to the current household and incapable of mutating data.
/// </summary>
public interface IAssistantTool
{
    /// <summary>Stable identifier advertised to the model and used to dispatch its calls.</summary>
    string Name { get; }

    /// <summary>Natural-language description that tells the model when to use the tool.</summary>
    string Description { get; }

    /// <summary>JSON-schema object describing the tool's parameters (provider-agnostic).</summary>
    object ParametersSchema { get; }

    /// <summary>Runs the tool with the model-supplied <paramref name="arguments"/>.</summary>
    Task<AssistantToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}
