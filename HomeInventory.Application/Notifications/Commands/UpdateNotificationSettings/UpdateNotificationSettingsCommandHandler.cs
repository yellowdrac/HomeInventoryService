using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Notifications.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Notifications.Commands.UpdateNotificationSettings;

public sealed class UpdateNotificationSettingsCommandHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, Result<NotificationSettingsDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public UpdateNotificationSettingsCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<NotificationSettingsDto>> Handle(
        UpdateNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var alertWindowDays = Math.Clamp(request.AlertWindowDays, 1, 14);

        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new NotificationSettings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmailEnabled = request.EmailEnabled,
                EmailAddress = request.EmailAddress,
                AlertWindowDays = alertWindowDays,
                CreatedAt = DateTime.UtcNow,
            };
            _context.NotificationSettings.Add(settings);
        }
        else
        {
            settings.EmailEnabled = request.EmailEnabled;
            settings.EmailAddress = request.EmailAddress;
            settings.AlertWindowDays = alertWindowDays;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new NotificationSettingsDto(
            settings.EmailEnabled,
            settings.EmailAddress,
            settings.AlertWindowDays));
    }
}
