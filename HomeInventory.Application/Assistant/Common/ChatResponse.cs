namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// The assistant's reply: the free-text <see cref="Answer"/> plus the items/locations it cited
/// (<see cref="References"/>) so the client can link them.
/// </summary>
public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<AssistantReference> References);
