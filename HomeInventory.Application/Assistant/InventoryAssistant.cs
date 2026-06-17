using System.Text.Json;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Assistant.Llm;
using HomeInventory.Application.Assistant.Tools;

namespace HomeInventory.Application.Assistant;

/// <summary>
/// Orchestrates the read-only tool-calling loop: it advertises the inventory tools to the LLM,
/// executes the tools the model asks for (each wrapping an existing, household-scoped MediatR query),
/// feeds the results back, and repeats until the model produces a final answer or the iteration cap
/// is reached. The concrete LLM provider is hidden behind <see cref="ILlmChatClient"/>.
/// </summary>
public sealed class InventoryAssistant : IInventoryAssistant
{
    /// <summary>Returned when the model keeps requesting tools past the iteration cap.</summary>
    public const string IterationLimitAnswer =
        "I'm sorry, I couldn't complete that request right now. Please try rephrasing your question.";

    private readonly ILlmChatClient _client;
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    private readonly IReadOnlyList<LlmToolDefinition> _toolDefinitions;
    private readonly AssistantOptions _options;
    private readonly IProposedActionsCollector _collector;

    public InventoryAssistant(
        ILlmChatClient client,
        IEnumerable<IAssistantTool> tools,
        AssistantOptions options,
        IProposedActionsCollector collector)
    {
        _client = client;
        _options = options;
        _collector = collector;
        _tools = tools.ToDictionary(t => t.Name);
        _toolDefinitions = _tools.Values
            .Select(t => new LlmToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();
    }

    public async Task<ChatResponse> AskAsync(
        string message,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken)
    {
        var messages = new List<LlmMessage>();
        foreach (var turn in history ?? [])
        {
            var role = string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? LlmRole.Assistant
                : LlmRole.User;
            messages.Add(new LlmMessage(role, turn.Content));
        }

        messages.Add(new LlmMessage(LlmRole.User, message));

        var references = new List<AssistantReference>();

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var request = new LlmRequest(
                AssistantSystemPrompt.Text,
                messages,
                _toolDefinitions,
                _options.MaxResponseTokens);

            var response = await _client.CompleteAsync(request, cancellationToken);

            if (!response.RequiresToolExecution)
            {
                return BuildResponse(response.Text ?? string.Empty, references);
            }

            // Record the assistant's tool-call turn, then run each requested tool and feed the
            // results back so the model can continue reasoning.
            messages.Add(new LlmMessage(LlmRole.Assistant, response.Text, response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                var toolResult = await ExecuteToolAsync(call, cancellationToken);
                references.AddRange(toolResult.References);
                messages.Add(new LlmMessage(
                    LlmRole.Tool,
                    toolResult.Content,
                    ToolCallId: call.Id,
                    ToolName: call.Name));
            }
        }

        // The model never settled on an answer within the iteration budget.
        return BuildResponse(IterationLimitAnswer, references);
    }

    private async Task<AssistantToolResult> ExecuteToolAsync(
        LlmToolCall call,
        CancellationToken cancellationToken)
    {
        if (!_tools.TryGetValue(call.Name, out var tool))
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = $"Unknown tool '{call.Name}'." }));
        }

        JsonElement arguments;
        try
        {
            // Deserialize into a self-contained JsonElement (no JsonDocument to keep alive/dispose).
            arguments = string.IsNullOrWhiteSpace(call.ArgumentsJson)
                ? default
                : JsonSerializer.Deserialize<JsonElement>(call.ArgumentsJson);
        }
        catch (JsonException)
        {
            return AssistantToolResult.FromContent(
                AssistantToolJson.Serialize(new { error = "The tool arguments were not valid JSON." }));
        }

        return await tool.ExecuteAsync(arguments, cancellationToken);
    }

    private ChatResponse BuildResponse(string answer, List<AssistantReference> references)
    {
        var actions = _collector.Actions.Count > 0 ? _collector.Actions : null;
        return new ChatResponse(answer, Deduplicate(references), actions, _collector.ClarificationQuestion);
    }

    private static IReadOnlyList<AssistantReference> Deduplicate(
        IEnumerable<AssistantReference> references) =>
        references
            .GroupBy(r => (r.Kind, r.Id))
            .Select(g => g.First())
            .ToList();
}
