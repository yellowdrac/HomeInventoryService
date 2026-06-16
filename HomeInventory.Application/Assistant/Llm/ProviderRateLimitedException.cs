namespace HomeInventory.Application.Assistant.Llm;

/// <summary>Thrown when the upstream LLM provider returns HTTP 429 (rate limit exceeded).</summary>
public sealed class ProviderRateLimitedException()
    : Exception("The LLM provider's API rate limit has been exceeded.");
