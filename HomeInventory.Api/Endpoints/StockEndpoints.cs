using HomeInventory.Api.Extensions;
using HomeInventory.Application.Stock.Commands.AddStock;
using HomeInventory.Application.Stock.Commands.ConsumeStock;
using HomeInventory.Application.Stock.Commands.DeleteStockLot;
using HomeInventory.Application.Stock.Commands.DiscardStock;
using HomeInventory.Application.Stock.Commands.MoveStock;
using HomeInventory.Application.Stock.Commands.UpdateStockLot;
using HomeInventory.Application.Stock.Queries.GetLocationContents;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var items = app.MapGroup("/api/items").WithTags("Stock").RequireAuthorization();

        items.MapPost("/{itemId:guid}/stock", async (
            Guid itemId, AddStockRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new AddStockCommand(
                    itemId, body.LocationId, body.Quantity, body.ExpirationDate, body.AcquiredDate),
                ct)).ToHttpResult());

        var lots = app.MapGroup("/api/stock-lots").WithTags("Stock").RequireAuthorization();

        lots.MapPut("/{id:guid}", async (
            Guid id, UpdateStockLotRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new UpdateStockLotCommand(id, body.Quantity, body.ExpirationDate, body.AcquiredDate),
                ct)).ToHttpResult());

        lots.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteStockLotCommand(id), ct)).ToHttpResult());

        lots.MapPost("/{id:guid}/move", async (
            Guid id, MoveStockRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new MoveStockCommand(id, body.ToLocationId, body.Quantity), ct)).ToHttpResult());

        lots.MapPost("/{id:guid}/consume", async (
            Guid id, ConsumeStockRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ConsumeStockCommand(id, body.Quantity, body.Reason), ct)).ToHttpResult());

        lots.MapPost("/{id:guid}/discard", async (
            Guid id, DiscardStockRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DiscardStockCommand(id, body.Quantity, body.Reason), ct)).ToHttpResult());

        var locations = app.MapGroup("/api/locations").WithTags("Stock").RequireAuthorization();

        locations.MapGet("/{id:guid}/contents", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetLocationContentsQuery(id), ct)).ToHttpResult());

        return app;
    }
}

/// <summary>Body of <c>POST /api/items/{itemId}/stock</c>; the item id travels in the route.</summary>
public sealed record AddStockRequest(
    Guid LocationId,
    decimal Quantity,
    DateOnly? ExpirationDate,
    DateOnly? AcquiredDate);

/// <summary>Body of <c>PUT /api/stock-lots/{id}</c>; the id travels in the route.</summary>
public sealed record UpdateStockLotRequest(
    decimal Quantity,
    DateOnly? ExpirationDate,
    DateOnly? AcquiredDate);

/// <summary>Body of <c>POST /api/stock-lots/{id}/move</c>; the lot id travels in the route.</summary>
public sealed record MoveStockRequest(Guid ToLocationId, decimal Quantity);

/// <summary>Body of <c>POST /api/stock-lots/{id}/consume</c>; the lot id travels in the route.</summary>
public sealed record ConsumeStockRequest(decimal Quantity, string? Reason);

/// <summary>Body of <c>POST /api/stock-lots/{id}/discard</c>; the lot id travels in the route.</summary>
public sealed record DiscardStockRequest(decimal Quantity, string? Reason);
