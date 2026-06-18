using HomeInventory.Domain.Common;

namespace HomeInventory.Domain.Entities;

/// <summary>
/// Web Push subscription for a specific user device. Not household-scoped — per user/device.
/// </summary>
public class PushSubscription : BaseEntity
{
    public Guid UserId { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string P256dhKey { get; set; } = string.Empty;

    public string AuthKey { get; set; } = string.Empty;
}
