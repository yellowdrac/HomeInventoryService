namespace HomeInventory.Application.Assistant.Llm;

/// <summary>
/// Provider-agnostic, single round-trip contract to a tool-calling LLM. The concrete implementation
/// (which provider, API key and model are read from configuration) lives in Infrastructure, so the
/// provider can be swapped (Anthropic / OpenAI / Gemini) without touching the Application layer.
/// </summary>
public interface ILlmChatClient
{
    /// <summary>
    /// Sends one request to the model and returns its reply, which is either a final answer or a
    /// request to execute one or more tools.
    /// </summary>
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken);
}
