using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Notifications.Commands.RemovePushSubscription;

public sealed record RemovePushSubscriptionCommand : IRequest<Result>;
