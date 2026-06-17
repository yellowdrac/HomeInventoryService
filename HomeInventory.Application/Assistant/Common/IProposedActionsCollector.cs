namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// Scoped accumulator that proposal tools write into during a single assistant turn.
/// The orchestrator reads from it after the tool loop to attach proposed actions and
/// clarification questions to the final <see cref="ChatResponse"/>.
/// </summary>
public interface IProposedActionsCollector
{
    void Add(ProposedAction action);

    /// <summary>
    /// Records a clarification question. Only the first call takes effect; subsequent calls
    /// are ignored so that the first ambiguity encountered wins.
    /// </summary>
    void SetClarificationQuestion(ClarificationQuestion question);

    IReadOnlyList<ProposedAction> Actions { get; }

    ClarificationQuestion? ClarificationQuestion { get; }
}
