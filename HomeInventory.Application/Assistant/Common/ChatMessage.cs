namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// A single turn of the conversation history sent by the client. <see cref="Role"/> is either
/// <c>"user"</c> or <c>"assistant"</c>; any other value is treated as a user turn.
/// </summary>
public sealed record ChatMessage(string Role, string Content);
