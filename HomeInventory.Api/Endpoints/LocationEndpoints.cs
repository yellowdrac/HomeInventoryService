using HomeInventory.Api.Extensions;
using HomeInventory.Application.Locations.Commands.CreateLocation;
using HomeInventory.Application.Locations.Commands.DeleteLocation;
using HomeInventory.Application.Locations.Commands.MoveLocation;
using HomeInventory.Application.Locations.Commands.UpdateLocation;
using HomeInventory.Application.Locations.Queries.GetLocationById;
using HomeInventory.Application.Locations.Queries.GetLocationTree;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations").WithTags("Locations").RequireAuthorization();

        group.MapGet("/tree", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetLocationTreeQuery(), ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetLocationByIdQuery(id), ct)).ToHttpResult());

        group.MapPost("", async (CreateLocationCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult());

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateLocationRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateLocationCommand(id, body.Name, body.Type), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/move", async (
            Guid id, MoveLocationRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new MoveLocationCommand(id, body.NewParentId), ct)).ToHttpResult());

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteLocationCommand(id), ct)).ToHttpResult());

        return app;
    }
}

/// <summary>Body of <c>PUT /api/locations/{id}</c>; the id travels in the route.</summary>
public sealed record UpdateLocationRequest(string Name, LocationType Type);

/// <summary>Body of <c>POST /api/locations/{id}/move</c>; the id travels in the route.</summary>
public sealed record MoveLocationRequest(Guid? NewParentId);
