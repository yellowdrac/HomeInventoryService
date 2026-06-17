namespace HomeInventory.Infrastructure.Notifications;

/// <summary>
/// Configuration for email (Resend) and Web Push (VAPID) notification services.
/// Bind from the "Notifications" section in appsettings / user-secrets.
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string ResendApiKey { get; init; } = string.Empty;

    public string EmailFrom { get; init; } = "HomeInventory <noreply@example.com>";

    public string VapidPublicKey { get; init; } = string.Empty;

    public string VapidPrivateKey { get; init; } = string.Empty;

    public string VapidEmail { get; init; } = "mailto:admin@example.com";

    public string AppUrl { get; init; } = "http://localhost:3000";
}
