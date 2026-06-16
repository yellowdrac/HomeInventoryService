namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// A structured pointer to an item or location the assistant cited, so the client can render it as
/// a link. <see cref="Breadcrumb"/> is the human-readable path (root → location) when applicable.
/// </summary>
public sealed record AssistantReference(
    AssistantReferenceKind Kind,
    Guid Id,
    string Name,
    string? Breadcrumb = null);
