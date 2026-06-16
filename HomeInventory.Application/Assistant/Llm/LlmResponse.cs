namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// The model's reply to a single <see cref="LlmRequest"/>: any free-text <see cref="Text"/> and the
/// <see cref="ToolCalls"/> it wants executed. When <see cref="ToolCalls"/> is non-empty the caller
/// must run them and send the results back; otherwise <see cref="Text"/> is the final answer.
/// </summary>
public sealed record LlmResponse(
    string? Text,
    IReadOnlyList<LlmToolCall> ToolCalls)
{
    /// <summary>True when the model is asking for one or more tools to be executed.</summary>
    public bool RequiresToolExecution => ToolCalls.Count > 0;
}
