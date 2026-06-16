using HomeInventory.Application.Assistant.Common;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// The outcome of running an <see cref="IAssistantTool"/>: the JSON <see cref="Content"/> fed back to
/// the model and the items/locations the tool surfaced (<see cref="References"/>), accumulated by the
/// orchestrator so the final answer can cite them.
/// </summary>
public sealed record AssistantToolResult(
    string Content,
    IReadOnlyList<AssistantReference> References)
{
    public static AssistantToolResult FromContent(string content) =>
        new(content, []);
}
