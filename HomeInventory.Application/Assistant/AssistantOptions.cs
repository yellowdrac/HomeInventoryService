namespace HomeInventory.Application.Assistant;

/// <summary>
/// Cost-bounding knobs for the assistant, bound from the <c>Assistant</c> configuration section.
/// Provider, API key and model live alongside these in configuration but are read by the concrete
/// LLM client in Infrastructure, never by the Application layer.
/// </summary>
public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>Hard cap on tokens the model may produce per reply.</summary>
    public int MaxResponseTokens { get; set; } = 1024;

    /// <summary>Maximum number of LLM round-trips per question (bounds tool-calling cost).</summary>
    public int MaxToolIterations { get; set; } = 5;

    /// <summary>Maximum number of questions a single user may ask per minute.</summary>
    public int RateLimitPerMinute { get; set; } = 10;
}
