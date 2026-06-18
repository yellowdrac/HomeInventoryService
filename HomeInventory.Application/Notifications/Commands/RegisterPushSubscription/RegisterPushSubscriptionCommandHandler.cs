using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Results;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Notifications.Commands.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionCommandHandler
    : IRequestHandler<RegisterPushSubscriptionCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public RegisterPushSubscriptionCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> Handle(
        RegisterPushSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.Endpoint == request.Endpoint,
                cancellationToken);

        if (existing is not null)
        {
            existing.P256dhKey = request.P256dhKey;
            existing.AuthKey = request.AuthKey;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dhKey = request.P256dhKey,
                AuthKey = request.AuthKey,
                CreatedAt = DateTime.UtcNow,
            };
            _context.PushSubscriptions.Add(subscription);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
