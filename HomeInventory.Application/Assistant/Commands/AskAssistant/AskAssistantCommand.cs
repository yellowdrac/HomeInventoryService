using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Assistant.Commands.AskAssistant;

/// <summary>
/// Asks the read-only inventory assistant a question. <paramref name="History"/> is the optional
/// recent conversation (oldest first) used for context.
/// </summary>
public sealed record AskAssistantCommand(
    string Message,
    IReadOnlyList<ChatMessage>? History = null)
    : IRequest<Result<ChatResponse>>;
