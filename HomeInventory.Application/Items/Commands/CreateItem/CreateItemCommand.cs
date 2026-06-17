using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Application.Items.Commands.CreateItem;

/// <summary>
/// Creates an item. The server computes its <c>NormalizedName</c> (lower-cased, accent-stripped).
/// For <see cref="TrackingType.Quantity"/> a <paramref name="Unit"/> is recommended; for
/// <see cref="TrackingType.Unique"/> it is irrelevant. The photo is managed separately through the
/// item photo endpoints.
/// </summary>
public sealed record CreateItemCommand(
    string Name,
    string? Category,
    string? Barcode,
    TrackingType TrackingType,
    string? Unit,
    int? MinimumQuantity = null)
    : IRequest<Result<ItemDto>>;
