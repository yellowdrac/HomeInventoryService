using HomeInventory.Application.Assistant.Common;

namespace HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;

/// <summary>An item or location that was created or affected by the execute step.</summary>
public sealed record ExecutedEntityRef(AssistantReferenceKind Kind, Guid Id, string Name);
