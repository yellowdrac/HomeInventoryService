using System.Text.Json;
using FluentAssertions;
using HomeInventory.Application.Assistant;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Assistant.Llm;
using HomeInventory.Application.Assistant.Tools;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class InventoryAssistantTests
{
    private readonly ILlmChatClient _client = Substitute.For<ILlmChatClient>();
    private readonly AssistantOptions _options = new() { MaxToolIterations = 5, MaxResponseTokens = 512 };

    private InventoryAssistant BuildAssistant(params IAssistantTool[] tools) =>
        new(_client, tools, _options, new ProposedActionsCollector());

    /// <summary>Queues sequential LLM replies and records every request the orchestrator sends.</summary>
    private List<LlmRequest> StubLlm(params LlmResponse[] replies)
    {
        var requests = new List<LlmRequest>();
        var queue = new Queue<LlmResponse>(replies);
        _client.CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                requests.Add(call.Arg<LlmRequest>());
                return queue.Count > 0 ? queue.Dequeue() : replies[^1];
            });
        return requests;
    }

    private static LlmResponse ToolCall(string id, string name, string argsJson) =>
        new(null, [new LlmToolCall(id, name, argsJson)]);

    private static LlmResponse FinalAnswer(string text) => new(text, []);

    [Fact]
    public async Task Answers_directly_without_tools_when_the_model_does_not_request_any()
    {
        StubLlm(FinalAnswer("42 items."));
        var assistant = BuildAssistant(new FakeTool("search_inventory"));

        var response = await assistant.AskAsync("how many items?", [], CancellationToken.None);

        response.Answer.Should().Be("42 items.");
        response.References.Should().BeEmpty();
        await _client.Received(1).CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Runs_the_requested_tool_and_feeds_its_result_back_to_the_model()
    {
        var tool = new FakeTool(
            "search_inventory",
            new AssistantToolResult(
                "{\"items\":[]}",
                [new AssistantReference(AssistantReferenceKind.Location, Guid.NewGuid(), "Drawer", "Home > Drawer")]));

        var requests = StubLlm(
            ToolCall("toolu_1", "search_inventory", "{\"query\":\"pilas\"}"),
            FinalAnswer("Están en Home > Drawer."));

        var assistant = BuildAssistant(tool);

        var response = await assistant.AskAsync("¿dónde están mis pilas?", [], CancellationToken.None);

        // The correct tool ran with the model-supplied arguments.
        tool.Calls.Should().Be(1);
        AssistantToolJson.GetString(tool.LastArguments, "query").Should().Be("pilas");

        // Its result was fed back to the model on the next round-trip.
        var followUp = requests[1];
        var toolMessage = followUp.Messages.Single(m => m.Role == LlmRole.Tool);
        toolMessage.ToolCallId.Should().Be("toolu_1");
        toolMessage.Content.Should().Be("{\"items\":[]}");

        // The final answer and the tool's references surface to the caller.
        response.Answer.Should().Be("Están en Home > Drawer.");
        response.References.Should().ContainSingle(r => r.Name == "Drawer");
    }

    [Fact]
    public async Task Respects_the_iteration_limit_and_returns_a_final_answer()
    {
        // A misbehaving model that never stops asking for tools.
        _client.CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => ToolCall("toolu_x", "search_inventory", "{\"query\":\"x\"}"));

        var tool = new FakeTool("search_inventory");
        var assistant = BuildAssistant(tool);

        var response = await assistant.AskAsync("loop forever", [], CancellationToken.None);

        await _client.Received(_options.MaxToolIterations)
            .CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
        tool.Calls.Should().Be(_options.MaxToolIterations);
        response.Answer.Should().Be(InventoryAssistant.IterationLimitAnswer);
    }

    [Fact]
    public async Task Maps_the_conversation_history_into_the_request()
    {
        var requests = StubLlm(FinalAnswer("done"));
        var assistant = BuildAssistant(new FakeTool("search_inventory"));

        var history = new List<ChatMessage>
        {
            new("user", "hola"),
            new("assistant", "¡hola! ¿en qué ayudo?"),
        };

        await assistant.AskAsync("¿qué tengo en la nevera?", history, CancellationToken.None);

        var messages = requests[0].Messages;
        messages.Should().HaveCount(3);
        messages[0].Role.Should().Be(LlmRole.User);
        messages[0].Content.Should().Be("hola");
        messages[1].Role.Should().Be(LlmRole.Assistant);
        messages[1].Content.Should().Be("¡hola! ¿en qué ayudo?");
        messages[2].Role.Should().Be(LlmRole.User);
        messages[2].Content.Should().Be("¿qué tengo en la nevera?");
    }

    [Fact]
    public async Task Deduplicates_references_across_tool_calls()
    {
        var sharedId = Guid.NewGuid();
        var tool = new FakeTool(
            "search_inventory",
            new AssistantToolResult(
                "{}",
                [
                    new AssistantReference(AssistantReferenceKind.Item, sharedId, "Batteries"),
                    new AssistantReference(AssistantReferenceKind.Item, sharedId, "Batteries"),
                ]));
        StubLlm(
            ToolCall("toolu_1", "search_inventory", "{\"query\":\"a\"}"),
            FinalAnswer("ok"));

        var assistant = BuildAssistant(tool);

        var response = await assistant.AskAsync("find", [], CancellationToken.None);

        response.References.Should().ContainSingle(r => r.Id == sharedId);
    }

    [Fact]
    public async Task Reports_an_unknown_tool_to_the_model_without_crashing()
    {
        var requests = StubLlm(
            ToolCall("toolu_1", "definitely_not_a_tool", "{}"),
            FinalAnswer("handled"));

        var assistant = BuildAssistant(new FakeTool("search_inventory"));

        var response = await assistant.AskAsync("hi", [], CancellationToken.None);

        response.Answer.Should().Be("handled");
        var toolMessage = requests[1].Messages.Single(m => m.Role == LlmRole.Tool);
        toolMessage.Content.Should().Contain("Unknown tool");
    }

    /// <summary>A controllable in-memory tool, to exercise the orchestration loop in isolation.</summary>
    private sealed class FakeTool : IAssistantTool
    {
        private readonly AssistantToolResult _result;

        public FakeTool(string name, AssistantToolResult? result = null)
        {
            Name = name;
            _result = result ?? AssistantToolResult.FromContent("{}");
        }

        public string Name { get; }

        public string Description => "fake tool";

        public object ParametersSchema => new { type = "object", properties = new { } };

        public int Calls { get; private set; }

        public JsonElement LastArguments { get; private set; }

        public Task<AssistantToolResult> ExecuteAsync(
            JsonElement arguments,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastArguments = arguments;
            return Task.FromResult(_result);
        }
    }
}
