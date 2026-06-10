using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.DiscardStock;

/// <summary>
/// Discards a quantity from a stock lot (e.g. spoiled or broken). The quantity must be positive and
/// not exceed the lot; the lot is removed when it reaches zero. Records a <c>Discarded</c> movement.
/// </summary>
public sealed record DiscardStockCommand(
    Guid StockLotId,
    decimal Quantity,
    string? Reason)
    : IRequest<Result>;
