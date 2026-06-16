using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeInventory.Application.Assistant.Llm;
using Microsoft.Extensions.Logging;

namespace HomeInventory.Infrastructure.Assistant;

/// <summary>
/// <see cref="ILlmChatClient"/> implementation for the Anthropic Messages API with tool calling.
/// It is a thin, single-round-trip translator: it maps the provider-agnostic <see cref="LlmRequest"/>
/// onto Anthropic's wire format (system + tools + content blocks), POSTs it, and maps the response
/// back to an <see cref="LlmResponse"/>. The tool-calling loop itself lives in the Application layer,
/// so swapping this for an OpenAI/Gemini client requires no Application changes.
/// </summary>
public sealed class AnthropicChatClient : ILlmChatClient
{
    private readonly HttpClient _httpClient;
    private readonly AssistantProviderOptions _options;
    private readonly ILogger<AnthropicChatClient> _logger;

    public AnthropicChatClient(
        HttpClient httpClient,
        AssistantProviderOptions options,
        ILogger<AnthropicChatClient> logger)
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

        var body = BuildRequestBody(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", _options.AnthropicVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Anthropic Messages API returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                payload);
            throw new HttpRequestException(
                $"The assistant provider returned status {(int)response.StatusCode}.");
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
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = JsonSerializer.SerializeToNode(tool.ParametersSchema),
            });
        }

        return new JsonObject
        {
            ["model"] = _options.Model,
            ["max_tokens"] = request.MaxTokens,
            ["system"] = request.SystemPrompt,
            ["tools"] = tools,
            ["messages"] = BuildMessages(request.Messages),
        };
    }

    private static JsonArray BuildMessages(IReadOnlyList<LlmMessage> messages)
    {
        var result = new JsonArray();

        for (var i = 0; i < messages.Count;)
        {
            var message = messages[i];

            if (message.Role == LlmRole.Tool)
            {
                // Anthropic requires tool results in a single user message; merge the consecutive run.
                var content = new JsonArray();
                while (i < messages.Count && messages[i].Role == LlmRole.Tool)
                {
                    var toolMessage = messages[i];
                    content.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = toolMessage.ToolCallId,
                        ["content"] = toolMessage.Content ?? string.Empty,
                    });
                    i++;
                }

                result.Add(new JsonObject { ["role"] = "user", ["content"] = content });
                continue;
            }

            if (message.Role == LlmRole.Assistant)
            {
                result.Add(BuildAssistantMessage(message));
                i++;
                continue;
            }

            // User turn: plain text content.
            result.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = message.Content ?? string.Empty,
            });
            i++;
        }

        return result;
    }

    private static JsonObject BuildAssistantMessage(LlmMessage message)
    {
        // No tool calls -> a plain text turn is enough.
        if (message.ToolCalls is not { Count: > 0 })
        {
            return new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = message.Content ?? string.Empty,
            };
        }

        var content = new JsonArray();
        if (!string.IsNullOrEmpty(message.Content))
        {
            content.Add(new JsonObject { ["type"] = "text", ["text"] = message.Content });
        }

        foreach (var call in message.ToolCalls)
        {
            content.Add(new JsonObject
            {
                ["type"] = "tool_use",
                ["id"] = call.Id,
                ["name"] = call.Name,
                ["input"] = ParseInputOrEmpty(call.ArgumentsJson),
            });
        }

        return new JsonObject { ["role"] = "assistant", ["content"] = content };
    }

    private static JsonNode ParseInputOrEmpty(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(argumentsJson) ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private static LlmResponse ParseResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var text = new StringBuilder();
        var toolCalls = new List<LlmToolCall>();

        if (root.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
                switch (type)
                {
                    case "text" when block.TryGetProperty("text", out var textValue):
                        text.Append(textValue.GetString());
                        break;

                    case "tool_use":
                        var id = block.TryGetProperty("id", out var idValue) ? idValue.GetString() : null;
                        var name = block.TryGetProperty("name", out var nameValue)
                            ? nameValue.GetString()
                            : null;
                        var input = block.TryGetProperty("input", out var inputValue)
                            ? inputValue.GetRawText()
                            : "{}";

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            toolCalls.Add(new LlmToolCall(id, name, input));
                        }

                        break;
                }
            }
        }

        var finalText = text.Length > 0 ? text.ToString() : null;
        return new LlmResponse(finalText, toolCalls);
    }
}
