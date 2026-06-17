namespace HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;

/// <summary>
/// Returned by the execute step: the entities that were created or affected, each with its id,
/// name and kind so the client can build direct links to the items/locations detail pages.
/// </summary>
public sealed record ExecuteAssistantActionResult(IReadOnlyList<ExecutedEntityRef> CreatedEntities);
