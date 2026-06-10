using HomeInventory.Api.Extensions;
using HomeInventory.Application.Expirations.Commands.DiscardExpiredStock;
using HomeInventory.Application.Expirations.Queries.GetExpiringStock;
using HomeInventory.Application.Expirations.Queries.GetKitchenOverview;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class ExpirationEndpoints
{
    public static IEndpointRouteBuilder MapExpirationEndpoints(this IEndpointRouteBuilder app)
    {
        var expirations = app.MapGroup("/api/expirations").WithTags("Expirations").RequireAuthorization();

        expirations.MapGet("", async (
            int? withinDays,
            bool? includeExpired,
            Guid? locationId,
            string? category,
            DateOnly? asOfDate,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new GetExpiringStockQuery(
                    withinDays ?? 7, includeExpired ?? true, locationId, category, asOfDate),
                ct)).ToHttpResult());

        expirations.MapPost("/discard-expired", async (
            DiscardExpiredRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new DiscardExpiredStockCommand(body.LocationId, body.AsOfDate), ct)).ToHttpResult());

        var kitchen = app.MapGroup("/api/kitchen").WithTags("Expirations").RequireAuthorization();

        kitchen.MapGet("/overview", async (
            Guid? locationId,
            int? withinDays,
            DateOnly? asOfDate,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new GetKitchenOverviewQuery(locationId, withinDays ?? 7, asOfDate), ct)).ToHttpResult());

        return app;
    }
}

/// <summary>Body of <c>POST /api/expirations/discard-expired</c>; both fields are optional.</summary>
public sealed record DiscardExpiredRequest(Guid? LocationId, DateOnly? AsOfDate);
