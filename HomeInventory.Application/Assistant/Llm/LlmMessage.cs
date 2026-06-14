namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// A provider-agnostic conversation message. The concrete LLM client translates this into the
/// provider's wire format (e.g. Anthropic content blocks).
/// <list type="bullet">
/// <item><see cref="LlmRole.User"/>/<see cref="LlmRole.Assistant"/>: plain <see cref="Content"/> text.</item>
/// <item><see cref="LlmRole.Assistant"/> may also carry <see cref="ToolCalls"/> it requested.</item>
/// <item><see cref="LlmRole.Tool"/>: a tool result, with <see cref="ToolCallId"/> and <see cref="ToolName"/>
/// identifying the call it answers and <see cref="Content"/> holding the serialized result.</item>
/// </list>
/// </summary>
public sealed record LlmMessage(
    LlmRole Role,
    string? Content = null,
    IReadOnlyList<LlmToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? ToolName = null);
