using System.Net.Http.Json;
using System.Text;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Notifications.Common;
using Microsoft.Extensions.Options;

namespace HomeInventory.Infrastructure.Notifications;

/// <summary>
/// Sends transactional email via the Resend API (https://api.resend.com).
/// Uses a typed <see cref="HttpClient"/> registered by DI.
/// </summary>
public sealed class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly NotificationOptions _options;

    public ResendEmailService(HttpClient httpClient, IOptions<NotificationOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ResendApiKey);
    }

    public async Task SendExpirationAlertAsync(
        string toEmail,
        IReadOnlyList<ExpiringItemSummary> items,
        CancellationToken ct = default)
    {
        var html = BuildHtml(items);

        var payload = new
        {
            from = _options.EmailFrom,
            to = new[] { toEmail },
            subject = $"HomeInventory — {items.Count} item(s) expiring soon",
            html,
        };

        using var response = await _httpClient.PostAsJsonAsync("emails", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    private static string BuildHtml(IReadOnlyList<ExpiringItemSummary> items)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8" /></head>
            <body style="font-family:sans-serif;color:#333">
            <h2>Items expiring soon</h2>
            <table border="1" cellpadding="8" cellspacing="0" style="border-collapse:collapse;width:100%">
              <thead>
                <tr style="background:#f5f5f5">
                  <th>Item</th>
                  <th>Location</th>
                  <th>Expiration date</th>
                  <th>Days until</th>
                </tr>
              </thead>
              <tbody>
            """);

        foreach (var item in items)
        {
            var daysLabel = item.DaysUntil < 0
                ? $"Expired {-item.DaysUntil} day(s) ago"
                : item.DaysUntil == 0
                    ? "Expires today"
                    : $"In {item.DaysUntil} day(s)";

            sb.Append($"""
                  <tr>
                    <td>{System.Web.HttpUtility.HtmlEncode(item.ItemName)}</td>
                    <td>{System.Web.HttpUtility.HtmlEncode(item.LocationName)}</td>
                    <td>{item.ExpirationDate:yyyy-MM-dd}</td>
                    <td>{daysLabel}</td>
                  </tr>
                """);
        }

        sb.Append("""
              </tbody>
            </table>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
