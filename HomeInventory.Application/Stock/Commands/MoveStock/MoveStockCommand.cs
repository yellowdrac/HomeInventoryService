using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.MoveStock;

/// <summary>
/// Moves a quantity from a stock lot to another location. The quantity must be positive and not
/// exceed the lot; for unique-tracked items the whole lot must be moved. When a lot of the same item
/// with the same expiration date already exists at the destination the quantity is merged into it,
/// otherwise a new lot is created. Returns the destination lot.
/// </summary>
public sealed record MoveStockCommand(
    Guid StockLotId,
    Guid ToLocationId,
    decimal Quantity)
    : IRequest<Result<StockLotDto>>;
