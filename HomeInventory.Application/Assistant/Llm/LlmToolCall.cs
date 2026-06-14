namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// A tool invocation requested by the model: the provider-assigned <see cref="Id"/> (used to
/// correlate the result), the tool <see cref="Name"/> and the raw JSON <see cref="ArgumentsJson"/>.
/// </summary>
public sealed record LlmToolCall(string Id, string Name, string ArgumentsJson);
