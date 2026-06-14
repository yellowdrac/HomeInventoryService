using HomeInventory.Application.Assistant.Common;

namespace HomeInventory.Application.Assistant;

/// <summary>
/// Read-only natural-language assistant over the current household's inventory. Given the user's
/// message and the recent conversation history, it answers using only the data exposed by its
/// (read-only) tools and returns the reply plus the items/locations it cited.
/// </summary>
public interface IInventoryAssistant
{
    Task<ChatResponse> AskAsync(
        string message,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken);
}
