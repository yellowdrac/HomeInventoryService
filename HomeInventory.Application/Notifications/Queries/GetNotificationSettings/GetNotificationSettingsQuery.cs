using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Notifications.Common;
using MediatR;

namespace HomeInventory.Application.Notifications.Queries.GetNotificationSettings;

public sealed record GetNotificationSettingsQuery : IRequest<Result<NotificationSettingsDto>>;
