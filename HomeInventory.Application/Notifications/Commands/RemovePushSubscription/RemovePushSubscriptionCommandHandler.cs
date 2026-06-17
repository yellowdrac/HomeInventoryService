using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Notifications.Commands.RemovePushSubscription;

public sealed class RemovePushSubscriptionCommandHandler
    : IRequestHandler<RemovePushSubscriptionCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public RemovePushSubscriptionCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> Handle(
        RemovePushSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var subscriptions = await _context.PushSubscriptions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.PushSubscriptions.RemoveRange(subscriptions);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
