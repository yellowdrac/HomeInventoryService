namespace HomeInventory.Application.Assistant.Common;

/// <summary>Describes an entity that does not yet exist and must be created as a sub-step.</summary>
public sealed record MissingEntity(string Kind, string Name);
