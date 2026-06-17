using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;

/// <summary>
/// Executes a list of assistant-proposed write actions in order (CreateLocation → CreateItem →
/// AddStock / MoveStock). Every action is re-validated server-side against the current household
/// before dispatching the real write commands. Returns the entities created during this execution.
/// </summary>
public sealed record ExecuteAssistantActionCommand(IReadOnlyList<ProposedAction> Actions)
    : IRequest<Result<ExecuteAssistantActionResult>>;
