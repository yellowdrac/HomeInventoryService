using HomeInventory.Api.Extensions;
using HomeInventory.Application.Households.Commands.CreateHousehold;
using HomeInventory.Application.Households.Commands.JoinHousehold;
using HomeInventory.Application.Households.Commands.RegenerateJoinCode;
using HomeInventory.Application.Households.Queries.GetMyHousehold;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/households").WithTags("Households").RequireAuthorization();

        group.MapPost("", async (CreateHouseholdCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult());

        group.MapPost("/join", async (JoinHouseholdCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult());

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyHouseholdQuery(), ct)).ToHttpResult());

        group.MapPost("/regenerate-code", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new RegenerateJoinCodeCommand(), ct)).ToHttpResult());

        return app;
    }
}
