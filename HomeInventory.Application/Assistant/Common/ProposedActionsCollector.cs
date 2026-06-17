namespace HomeInventory.Application.Assistant.Common;

public sealed class ProposedActionsCollector : IProposedActionsCollector
{
    private readonly List<ProposedAction> _actions = [];

    public void Add(ProposedAction action) => _actions.Add(action);

    public void SetClarificationQuestion(ClarificationQuestion question)
    {
        ClarificationQuestion ??= question;
    }

    public IReadOnlyList<ProposedAction> Actions => _actions;

    public ClarificationQuestion? ClarificationQuestion { get; private set; }
}
