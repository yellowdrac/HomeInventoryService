using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.UpdateStockLot;

/// <summary>
/// Adjusts the quantity and dates of a stock lot. The lot stays at its current location
/// (relocation is handled in a later phase).
/// </summary>
public sealed record UpdateStockLotCommand(
    Guid Id,
    decimal Quantity,
    DateOnly? ExpirationDate,
    DateOnly? AcquiredDate)
    : IRequest<Result<StockLotDto>>;
