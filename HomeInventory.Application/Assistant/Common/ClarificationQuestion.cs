namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// Returned when the assistant needs the user to disambiguate between multiple matching entities.
/// The client renders each <see cref="Options"/> entry as a quick-reply button.
/// </summary>
public sealed record ClarificationQuestion(string Text, IReadOnlyList<string> Options);
