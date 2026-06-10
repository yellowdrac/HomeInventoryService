using HomeInventory.Api.Extensions;
using HomeInventory.Application.Movements.Queries.GetMovements;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class MovementEndpoints
{
    public static IEndpointRouteBuilder MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/movements").WithTags("Movements").RequireAuthorization();

        group.MapGet("", async (
            Guid? itemId,
            Guid? locationId,
            MovementType? type,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new GetMovementsQuery(
                    itemId, locationId, type, dateFrom, dateTo, page ?? 1, pageSize ?? 20),
                ct)).ToHttpResult());

        return app;
    }
}
