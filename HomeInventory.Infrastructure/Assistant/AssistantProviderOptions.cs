namespace HomeInventory.Infrastructure.Assistant;

/// <summary>
/// LLM provider settings bound from the <c>Assistant</c> configuration section. The API key is read
/// from configuration/environment only (<c>Assistant:ApiKey</c> / <c>Assistant__ApiKey</c>) and is
/// never hardcoded or committed.
/// </summary>
public sealed class AssistantProviderOptions
{
    /// <summary>Which provider to use. Currently only <c>Anthropic</c> is implemented.</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Provider API key. Supplied via env var or user-secrets, never the repo.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model id. Defaults to an economical tool-calling model.</summary>
    public string Model { get; set; } = "claude-haiku-4-5";

    /// <summary>Messages endpoint. Overridable to point at a gateway or a compatible provider.</summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/messages";

    /// <summary>Anthropic API version header value.</summary>
    public string AnthropicVersion { get; set; } = "2023-06-01";
}
