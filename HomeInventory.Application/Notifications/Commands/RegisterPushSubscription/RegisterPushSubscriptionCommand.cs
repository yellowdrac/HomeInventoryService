using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Notifications.Commands.RegisterPushSubscription;

public sealed record RegisterPushSubscriptionCommand(
    string Endpoint,
    string P256dhKey,
    string AuthKey) : IRequest<Result>;
