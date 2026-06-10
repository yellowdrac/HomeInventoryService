using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.AddStock;

/// <summary>
/// Adds a stock lot of an item at a location. Quantity must be positive. For unique-tracked items
/// the quantity is forced to 1 and a second lot is rejected.
/// </summary>
public sealed record AddStockCommand(
    Guid ItemId,
    Guid LocationId,
    decimal Quantity,
    DateOnly? ExpirationDate,
    DateOnly? AcquiredDate)
    : IRequest<Result<StockLotDto>>;
