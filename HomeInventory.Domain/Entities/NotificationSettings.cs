using HomeInventory.Domain.Common;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Per-user notification preferences. Not household-scoped — one row per user.
/// </summary>
public class NotificationSettings : BaseEntity
{
    public Guid UserId { get; set; }

    public bool EmailEnabled { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>Alert when items expire within this many days (default 3, clamped 1–14).</summary>
    public int AlertWindowDays { get; set; } = 3;
}
