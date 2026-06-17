using HomeInventory.Application.Notifications.Common;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Sends email notifications about expiring inventory items.
/// </summary>
public interface IEmailService
{
    Task SendExpirationAlertAsync(
        string toEmail,
        IReadOnlyList<ExpiringItemSummary> items,
        CancellationToken ct = default);
}
