namespace HomeInventory.Application.Notifications.Common;

/// <summary>DTO returned for notification settings queries and commands.</summary>
public sealed record NotificationSettingsDto(bool EmailEnabled, string EmailAddress, int AlertWindowDays);
