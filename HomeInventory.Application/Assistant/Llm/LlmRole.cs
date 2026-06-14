namespace HomeInventory.Application.Assistant.Llm;

/// <summary>Author of an <see cref="LlmMessage"/> in a provider-agnostic conversation.</summary>
public enum LlmRole
{
    User,
    Assistant,

    /// <summary>The result of a tool the assistant requested, fed back into the conversation.</summary>
    Tool,
}
