using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Stock.Commands.DeleteStockLot;

/// <summary>Deletes a stock lot.</summary>
public sealed record DeleteStockLotCommand(Guid Id) : IRequest<Result>;
