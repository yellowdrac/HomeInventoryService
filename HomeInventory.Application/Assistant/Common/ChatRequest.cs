namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// Payload for a chat turn: the user's <see cref="Message"/> and the optional recent
/// conversation <see cref="History"/> (oldest first) used to give the assistant context.
/// </summary>
public sealed record ChatRequest(
    string Message,
    IReadOnlyList<ChatMessage>? History = null);
