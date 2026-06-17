using System.Net;
using HomeInventory.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace HomeInventory.Infrastructure.Notifications;

/// <summary>
/// Sends Web Push notifications via VAPID using the <c>WebPush</c> NuGet package.
/// Registered as a singleton — <see cref="WebPushClient"/> is thread-safe.
/// </summary>
public sealed class WebPushNotificationService : IPushNotificationService
{
    private readonly WebPushClient _client;
    private readonly NotificationOptions _options;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        IOptions<NotificationOptions> options,
        ILogger<WebPushNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new WebPushClient();

        if (!string.IsNullOrWhiteSpace(_options.VapidPublicKey)
            && !string.IsNullOrWhiteSpace(_options.VapidPrivateKey))
        {
            _client.SetVapidDetails(
                _options.VapidEmail,
                _options.VapidPublicKey,
                _options.VapidPrivateKey);
        }
    }

    public async Task SendAsync(
        string endpoint,
        string p256dhKey,
        string authKey,
        string title,
        string body,
        string url = "/kitchen",
        CancellationToken ct = default)
    {
        var subscription = new WebPush.PushSubscription(endpoint, p256dhKey, authKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title,
            body,
            url = $"{_options.AppUrl.TrimEnd('/')}{url}",
        });

        try
        {
            await _client.SendNotificationAsync(subscription, payload);
        }
        catch (WebPushException ex)
            when (ex.StatusCode == HttpStatusCode.Gone
                  || ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Web Push subscription expired or not found for endpoint {Endpoint}. StatusCode={StatusCode}",
                endpoint,
                ex.StatusCode);
        }
    }
}
