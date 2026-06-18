using HomeInventory.Api.Extensions;
using HomeInventory.Application.Notifications.Commands.RegisterPushSubscription;
using HomeInventory.Application.Notifications.Commands.RemovePushSubscription;
using HomeInventory.Application.Notifications.Commands.UpdateNotificationSettings;
using HomeInventory.Application.Notifications.Queries.GetNotificationSettings;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/settings", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetNotificationSettingsQuery(), ct)).ToHttpResult());

        group.MapPut("/settings", async (
            UpdateNotificationSettingsCommand body,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(body, ct)).ToHttpResult());

        group.MapPost("/push-subscription", async (
            RegisterPushSubscriptionCommand body,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(body, ct)).ToHttpResult());

        group.MapDelete("/push-subscription", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemovePushSubscriptionCommand(), ct)).ToHttpResult());

        return app;
    }
}
