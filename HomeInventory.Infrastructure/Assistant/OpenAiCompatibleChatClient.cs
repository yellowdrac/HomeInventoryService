using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeInventory.Application.Assistant.Llm;
using Microsoft.Extensions.Logging;

namespace HomeInventory.Infrastructure.Assistant;

/// <summary>
/// <see cref="ILlmChatClient"/> for any provider exposing an OpenAI-compatible
/// <c>/chat/completions</c> endpoint with function/tool calling — Google Gemini (OpenAI-compat
/// endpoint), Groq, Cerebras, OpenRouter, Mistral, DeepSeek, Ollama, etc. Like the Anthropic client
/// it is a thin single-round-trip translator; the tool-calling loop stays in the Application layer.
/// </summary>
public sealed class OpenAiCompatibleChatClient : ILlmChatClient
{
    private readonly HttpClient _httpClient;
    private readonly AssistantProviderOptions _options;
    private readonly ILogger<OpenAiCompatibleChatClient> _logger;

    public OpenAiCompatibleChatClient(
        HttpClient httpClient,
        AssistantProviderOptions options,
        ILogger<OpenAiCompatibleChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "The assistant API key is not configured. Set 'Assistant:ApiKey'.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException(
                "The assistant base URL is not configured. Set 'Assistant:BaseUrl' to the provider's "
                + "OpenAI-compatible /chat/completions endpoint.");
        }

        var body = BuildRequestBody(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenAI-compatible provider returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                payload);

            if ((int)response.StatusCode == 429)
            {
                throw new ProviderRateLimitedException();
            }

            throw new HttpRequestException(
                $"The assistant provider returned status {(int)response.StatusCode}.");
        }

        // Log the model that actually handled the request.
        // Useful when the configured model is a routing alias (e.g. "openrouter/free")
        // because the response always contains the real model that was selected.
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("model", out var modelEl)
                && modelEl.ValueKind == JsonValueKind.String)
            {
                var resolvedModel = modelEl.GetString();
                if (resolvedModel != _options.Model)
                {
                    _logger.LogInformation(
                        "Model alias '{Alias}' resolved to '{Model}'",
                        _options.Model,
                        resolvedModel);
                }
                else
                {
                    _logger.LogDebug("Model: {Model}", resolvedModel);
                }
            }
        }
        catch
        {
            // Non-critical — never let logging crash the response path.
        }

        return ParseResponse(payload);
    }

    private JsonObject BuildRequestBody(LlmRequest request)
    {
        var tools = new JsonArray();
        foreach (var tool in request.Tools)
        {
            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonSerializer.SerializeToNode(tool.ParametersSchema),
                },
            });
        }

        var body = new JsonObject
        {
            ["model"] = _options.Model,
            ["max_tokens"] = request.MaxTokens,
            ["messages"] = BuildMessages(request),
        };

        if (tools.Count > 0)
        {
            body["tools"] = tools;
            body["tool_choice"] = "auto";
        }

        return body;
    }

    private static JsonArray BuildMessages(LlmRequest request)
    {
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = request.SystemPrompt },
        };

        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case LlmRole.User:
                    messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = message.Content ?? string.Empty,
                    });
                    break;

                case LlmRole.Assistant:
                    messages.Add(BuildAssistantMessage(message));
                    break;

                case LlmRole.Tool:
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = message.ToolCallId,
                        ["content"] = message.Content ?? string.Empty,
                    });
                    break;
            }
        }

        return messages;
    }

    private static JsonObject BuildAssistantMessage(LlmMessage message)
    {
        var hasToolCalls = message.ToolCalls is { Count: > 0 };

        var assistant = new JsonObject { ["role"] = "assistant" };

        // The OpenAI spec requires content=null (not "") when tool_calls is present.
        // Sending an empty string causes several providers (Cohere, Nvidia) to reject the message.
        if (!hasToolCalls || !string.IsNullOrEmpty(message.Content))
        {
            assistant["content"] = message.Content;
        }
        else
        {
            assistant["content"] = JsonValue.Create((string?)null);
        }

        if (hasToolCalls)
        {
            var toolCalls = new JsonArray();
            foreach (var call in message.ToolCalls!)
            {
                toolCalls.Add(new JsonObject
                {
                    ["id"] = call.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = call.Name,
                        ["arguments"] = JsonValue.Create(
                            string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson),
                    },
                });
            }

            assistant["tool_calls"] = toolCalls;
        }

        return assistant;
    }

    private static LlmResponse ParseResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            return new LlmResponse(null, []);
        }

        var message = choices[0].GetProperty("message");

        string? text = null;
        if (message.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String)
        {
            var value = content.GetString();
            text = string.IsNullOrEmpty(value) ? null : value;
        }

        var toolCalls = new List<LlmToolCall>();
        if (message.TryGetProperty("tool_calls", out var calls)
            && calls.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in calls.EnumerateArray())
            {
                var id = call.TryGetProperty("id", out var idValue) ? idValue.GetString() : null;
                if (!call.TryGetProperty("function", out var function))
                {
                    continue;
                }

                var name = function.TryGetProperty("name", out var nameValue)
                    ? nameValue.GetString()
                    : null;
                // Some providers return arguments as a JSON string (standard OpenAI format);
                // others return a raw JSON object. Handle both so the tool-calling loop
                // always receives a non-null JSON string it can deserialize.
                string arguments;
                if (function.TryGetProperty("arguments", out var argsValue))
                {
                    arguments = argsValue.ValueKind switch
                    {
                        JsonValueKind.String => argsValue.GetString() ?? "{}",
                        JsonValueKind.Object or JsonValueKind.Array => argsValue.GetRawText(),
                        _ => "{}",
                    };
                }
                else
                {
                    arguments = "{}";
                }

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                {
                    toolCalls.Add(new LlmToolCall(id, name, arguments));
                }
            }
        }

        return new LlmResponse(text, toolCalls);
    }
}
