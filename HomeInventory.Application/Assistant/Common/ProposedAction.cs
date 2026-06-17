namespace HomeInventory.Application.Assistant.Common;

/// <summary>
/// A single inventory mutation proposed by the AI. The mutation is never executed by the proposal
/// tool itself — the client confirms it and sends it to <c>POST /api/assistant/execute</c> which
/// re-validates everything before dispatching the real write commands.
///
/// Nullable fields carry the data needed for each action type:
///   CreateLocation → LocationName, LocationTypeName, Parent* fields.
///   CreateItem     → ItemName, ItemCategory, ItemTrackingTypeName, ItemUnit.
///   AddStock       → ResolvedItemId|UnresolvedItemName, ResolvedLocationId|UnresolvedLocationName, Quantity.
///   MoveStock      → ResolvedItemId, Resolved(From|To)LocationId, Quantity.
/// </summary>
public sealed record ProposedAction(
    ProposedActionType Type,
    IReadOnlyList<MissingEntity> MissingEntities,
    string Summary,
    bool HasDuplicateWarning = false,
    // CreateLocation
    string? LocationName = null,
    string? LocationTypeName = null,
    Guid? ParentLocationId = null,
    string? ParentLocationName = null,
    // CreateItem
    string? ItemName = null,
    string? ItemCategory = null,
    string? ItemTrackingTypeName = null,
    string? ItemUnit = null,
    // AddStock / MoveStock – item reference
    Guid? ResolvedItemId = null,
    string? UnresolvedItemName = null,
    // AddStock – destination location (may be unresolved if creation is a prerequisite)
    Guid? ResolvedLocationId = null,
    string? UnresolvedLocationName = null,
    decimal? Quantity = null,
    DateOnly? ExpirationDate = null,
    // MoveStock – source and destination
    Guid? ResolvedFromLocationId = null,
    string? UnresolvedFromLocationName = null,
    Guid? ResolvedToLocationId = null,
    string? UnresolvedToLocationName = null);
