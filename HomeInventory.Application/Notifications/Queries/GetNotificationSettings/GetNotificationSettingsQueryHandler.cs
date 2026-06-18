using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Notifications.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Notifications.Queries.GetNotificationSettings;

public sealed class GetNotificationSettingsQueryHandler
    : IRequestHandler<GetNotificationSettingsQuery, Result<NotificationSettingsDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetNotificationSettingsQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<NotificationSettingsDto>> Handle(
        GetNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            return Result.Success(new NotificationSettingsDto(false, string.Empty, 3));
        }

        return Result.Success(new NotificationSettingsDto(
            settings.EmailEnabled,
            settings.EmailAddress,
            settings.AlertWindowDays));
    }
}
