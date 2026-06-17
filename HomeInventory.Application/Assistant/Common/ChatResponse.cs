namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// The assistant's reply: the free-text <see cref="Answer"/>, the items/locations it cited
/// (<see cref="References"/>), any write actions it proposes (<see cref="ProposedActions"/>), and
/// an optional disambiguation question (<see cref="ClarificationQuestion"/>) when the user must
/// choose between multiple matching entities before a proposal can be built.
/// </summary>
public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<AssistantReference> References,
    IReadOnlyList<ProposedAction>? ProposedActions = null,
    ClarificationQuestion? ClarificationQuestion = null);
