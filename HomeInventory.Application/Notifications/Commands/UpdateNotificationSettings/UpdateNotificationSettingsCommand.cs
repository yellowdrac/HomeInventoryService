using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Notifications.Common;
using MediatR;

namespace HomeInventory.Application.Notifications.Commands.UpdateNotificationSettings;

public sealed record UpdateNotificationSettingsCommand(
    bool EmailEnabled,
    string EmailAddress,
    int AlertWindowDays) : IRequest<Result<NotificationSettingsDto>>;
