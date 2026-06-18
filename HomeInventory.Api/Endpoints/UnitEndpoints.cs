using HomeInventory.Api.Extensions;
using HomeInventory.Application.Units;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class UnitEndpoints
{
    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/units", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUnitsQuery(), ct)).ToHttpResult())
            .WithTags("Units")
            .RequireAuthorization();
        return app;
    }
}
