using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Application.Items.Commands.UpdateItem;

/// <summary>
/// Updates an item's fields. Recomputes <c>NormalizedName</c> when the name changes. The photo is
/// managed separately through the item photo endpoints and is not touched here.
/// </summary>
public sealed record UpdateItemCommand(
    Guid Id,
    string Name,
    string? Category,
    string? Barcode,
    TrackingType TrackingType,
    string? Unit)
    : IRequest<Result<ItemDto>>;
