namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// A single request to the LLM: the <see cref="SystemPrompt"/>, the running <see cref="Messages"/>,
/// the <see cref="Tools"/> the model may call and the response token cap (<see cref="MaxTokens"/>).
/// </summary>
public sealed record LlmRequest(
    string SystemPrompt,
    IReadOnlyList<LlmMessage> Messages,
    IReadOnlyList<LlmToolDefinition> Tools,
    int MaxTokens);
