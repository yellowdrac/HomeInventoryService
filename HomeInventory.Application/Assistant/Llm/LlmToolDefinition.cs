namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// A tool advertised to the model: its <see cref="Name"/>, a natural-language <see cref="Description"/>
/// and a JSON-schema object describing its parameters (<see cref="ParametersSchema"/>). The schema is
/// kept as a plain serializable object so it stays provider-agnostic.
/// </summary>
public sealed record LlmToolDefinition(
    string Name,
    string Description,
    object ParametersSchema);
