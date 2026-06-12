using HomeInventory.Api.Extensions;
using HomeInventory.Application.Items.Commands.CreateItem;
using HomeInventory.Application.Items.Commands.DeleteItem;
using HomeInventory.Application.Items.Commands.DeleteItemPhoto;
using HomeInventory.Application.Items.Commands.UpdateItem;
using HomeInventory.Application.Items.Commands.UploadItemPhoto;
using HomeInventory.Application.Items.Queries.GetItemById;
using HomeInventory.Application.Items.Queries.GetItems;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items").RequireAuthorization();

        group.MapGet("", async (
            string? nameFilter,
            string? category,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new GetItemsQuery(nameFilter, category, page ?? 1, pageSize ?? 20), ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetItemByIdQuery(id), ct)).ToHttpResult());

        group.MapPost("", async (CreateItemCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult());

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateItemRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new UpdateItemCommand(
                    id, body.Name, body.Category, body.Barcode, body.TrackingType, body.Unit),
                ct)).ToHttpResult());

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteItemCommand(id), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/photo", async (
            Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadItemPhotoCommand(id, stream, file.ContentType, file.Length);
            return (await sender.Send(command, ct)).ToHttpResult();
        }).DisableAntiforgery();

        group.MapDelete("/{id:guid}/photo", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteItemPhotoCommand(id), ct)).ToHttpResult());

        return app;
    }
}

/// <summary>Body of <c>PUT /api/items/{id}</c>; the id travels in the route.</summary>
public sealed record UpdateItemRequest(
    string Name,
    string? Category,
    string? Barcode,
    TrackingType TrackingType,
    string? Unit);
