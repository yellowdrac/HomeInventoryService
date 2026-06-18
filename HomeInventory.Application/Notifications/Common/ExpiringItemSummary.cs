namespace HomeInventory.Application.Notifications.Common;

/// <summary>
/// Lightweight summary of an expiring inventory item used in notification payloads.
/// </summary>
public sealed record ExpiringItemSummary(
    string ItemName,
    string LocationName,
    DateOnly ExpirationDate,
    int DaysUntil);
