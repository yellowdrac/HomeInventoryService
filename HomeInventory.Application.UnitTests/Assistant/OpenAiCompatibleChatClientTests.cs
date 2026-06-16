using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HomeInventory.Application.Assistant.Llm;
using HomeInventory.Infrastructure.Assistant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class OpenAiCompatibleChatClientTests
{
    private static readonly LlmToolDefinition SearchTool = new(
        "search_inventory",
        "find items",
        new { type = "object", properties = new { query = new { type = "string" } }, required = new[] { "query" } });

    private static OpenAiCompatibleChatClient BuildClient(StubHandler handler) => new(
        new HttpClient(handler),
        new AssistantProviderOptions
        {
            Provider = "Gemini",
            ApiKey = "secret-key",
            Model = "gemini-2.5-flash",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
        },
        NullLogger<OpenAiCompatibleChatClient>.Instance);

    [Fact]
    public async Task Sends_bearer_auth_model_messages_and_tools_in_openai_shape()
    {
        var handler = new StubHandler(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"hi\"}}]}");
        var client = BuildClient(handler);

        var request = new LlmRequest(
            "SYSTEM",
            [new LlmMessage(LlmRole.User, "where are my batteries?")],
            [SearchTool],
            256);

        await client.CompleteAsync(request, CancellationToken.None);

        handler.CapturedAuth.Should().Be(new AuthenticationHeaderValue("Bearer", "secret-key"));

        using var body = JsonDocument.Parse(handler.CapturedBody!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be("gemini-2.5-flash");
        root.GetProperty("max_tokens").GetInt32().Should().Be(256);
        root.GetProperty("tool_choice").GetString().Should().Be("auto");

        var messages = root.GetProperty("messages");
        messages[0].GetProperty("role").GetString().Should().Be("system");
        messages[0].GetProperty("content").GetString().Should().Be("SYSTEM");
        messages[1].GetProperty("role").GetString().Should().Be("user");
        messages[1].GetProperty("content").GetString().Should().Be("where are my batteries?");

        var function = root.GetProperty("tools")[0].GetProperty("function");
        function.GetProperty("name").GetString().Should().Be("search_inventory");
        function.GetProperty("parameters").GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public async Task Parses_text_and_tool_calls_from_the_response()
    {
        const string response = """
            {
              "choices": [{
                "message": {
                  "role": "assistant",
                  "content": "let me look",
                  "tool_calls": [{
                    "id": "call_1",
                    "type": "function",
                    "function": { "name": "search_inventory", "arguments": "{\"query\":\"milk\"}" }
                  }]
                }
              }]
            }
            """;
        var client = BuildClient(new StubHandler(response));

        var result = await client.CompleteAsync(
            new LlmRequest("S", [new LlmMessage(LlmRole.User, "find milk")], [SearchTool], 256),
            CancellationToken.None);

        result.Text.Should().Be("let me look");
        result.RequiresToolExecution.Should().BeTrue();
        var call = result.ToolCalls.Should().ContainSingle().Subject;
        call.Id.Should().Be("call_1");
        call.Name.Should().Be("search_inventory");
        call.ArgumentsJson.Should().Be("{\"query\":\"milk\"}");
    }

    [Fact]
    public async Task Throws_when_the_provider_returns_an_error_status()
    {
        var client = BuildClient(new StubHandler("nope", HttpStatusCode.Unauthorized));

        var act = () => client.CompleteAsync(
            new LlmRequest("S", [new LlmMessage(LlmRole.User, "hi")], [SearchTool], 256),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _status;

        public StubHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseJson = responseJson;
            _status = status;
        }

        public string? CapturedBody { get; private set; }

        public AuthenticationHeaderValue? CapturedAuth { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedAuth = request.Headers.Authorization;
            CapturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
