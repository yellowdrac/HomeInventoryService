namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Sends Web Push notifications to a specific device subscription.
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(
        string endpoint,
        string p256dhKey,
        string authKey,
        string title,
        string body,
        string url = "/kitchen",
        CancellationToken ct = default);
}
