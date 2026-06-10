using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.ConsumeStock;

/// <summary>
/// Consumes a quantity from a stock lot. The quantity must be positive and not exceed the lot; the
/// lot is removed when it reaches zero. Records a <c>Consumed</c> movement.
/// </summary>
public sealed record ConsumeStockCommand(
    Guid StockLotId,
    decimal Quantity,
    string? Reason)
    : IRequest<Result>;
