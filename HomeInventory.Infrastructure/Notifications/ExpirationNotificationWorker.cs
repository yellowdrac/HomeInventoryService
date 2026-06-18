using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Notifications.Common;
using HomeInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeInventory.Infrastructure.Notifications;

/// <summary>
/// Background worker that runs every 24 hours and dispatches expiration notifications
/// (email and/or Web Push) to users whose items are about to expire.
/// </summary>
public sealed class ExpirationNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirationNotificationWorker> _logger;

    public ExpirationNotificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirationNotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error in ExpirationNotificationWorker.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var notificationOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<NotificationOptions>>().Value;

        // Collect user IDs that have email notifications enabled or have push subscriptions.
        var emailUserIds = await context.NotificationSettings
            .Where(s => s.EmailEnabled)
            .Select(s => s.UserId)
            .ToListAsync(ct);

        var pushUserIds = await context.PushSubscriptions
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        var allUserIds = emailUserIds.Union(pushUserIds).Distinct().ToList();

        if (allUserIds.Count == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Pre-load all notification settings and push subscriptions for these users.
        var settingsByUser = await context.NotificationSettings
            .Where(s => allUserIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, ct);

        var pushSubsByUser = await context.PushSubscriptions
            .Where(s => allUserIds.Contains(s.UserId))
            .ToListAsync(ct);

        foreach (var userId in allUserIds)
        {
            try
            {
                await ProcessUserAsync(
                    userId,
                    today,
                    settingsByUser,
                    pushSubsByUser,
                    context,
                    emailService,
                    pushService,
                    userManager,
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing notifications for user {UserId}.", userId);
            }
        }
    }

    private async Task ProcessUserAsync(
        Guid userId,
        DateOnly today,
        Dictionary<Guid, Domain.Entities.NotificationSettings> settingsByUser,
        List<Domain.Entities.PushSubscription> allPushSubs,
        IApplicationDbContext context,
        IEmailService emailService,
        IPushNotificationService pushService,
        UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.HouseholdId is not { } householdId)
        {
            return;
        }

        settingsByUser.TryGetValue(userId, out var settings);
        var alertWindowDays = settings?.AlertWindowDays ?? 3;
        var threshold = today.AddDays(alertWindowDays);

        // Use IgnoreQueryFilters because there is no HTTP context for the background service
        // (ICurrentUser.HouseholdId is null), which would cause the global filter to return nothing.
        var lots = await context.StockLots
            .IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId
                        && s.ExpirationDate != null
                        && s.ExpirationDate <= threshold
                        && s.ExpirationDate >= today)
            .ToListAsync(ct);

        if (lots.Count == 0)
        {
            return;
        }

        var lotLocationIds = lots.Select(l => l.LocationId).Distinct().ToList();
        var lotItemIds = lots.Select(l => l.ItemId).Distinct().ToList();

        var locationsById = await context.Locations
            .IgnoreQueryFilters()
            .Where(l => l.HouseholdId == householdId && lotLocationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        var itemsById = await context.Items
            .IgnoreQueryFilters()
            .Where(i => i.HouseholdId == householdId && lotItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var summaries = lots
            .Select(lot =>
            {
                var itemName = itemsById.TryGetValue(lot.ItemId, out var item) ? item.Name : string.Empty;
                var locationName = locationsById.TryGetValue(lot.LocationId, out var loc) ? loc.Name : string.Empty;
                var expiration = lot.ExpirationDate!.Value;
                var daysUntil = expiration.DayNumber - today.DayNumber;
                return new ExpiringItemSummary(itemName, locationName, expiration, daysUntil);
            })
            .OrderBy(s => s.ExpirationDate)
            .ToList();

        // Email notification.
        if (settings is { EmailEnabled: true } && !string.IsNullOrWhiteSpace(settings.EmailAddress))
        {
            try
            {
                await emailService.SendExpirationAlertAsync(settings.EmailAddress, summaries, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to send expiration email to user {UserId}.", userId);
            }
        }

        // Push notifications.
        var userPushSubs = allPushSubs.Where(p => p.UserId == userId).ToList();
        foreach (var sub in userPushSubs)
        {
            try
            {
                var title = $"{summaries.Count} item(s) expiring soon";
                var body = string.Join(", ", summaries.Take(3).Select(s => s.ItemName));
                await pushService.SendAsync(sub.Endpoint, sub.P256dhKey, sub.AuthKey, title, body, "/kitchen", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to send push notification to user {UserId} endpoint {Endpoint}.",
                    userId,
                    sub.Endpoint);
            }
        }
    }
}
