using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Stock.Queries.GetLocationContents;

/// <summary>Returns the stock lots stored at a location (with their item data).</summary>
public sealed record GetLocationContentsQuery(Guid LocationId)
    : IRequest<Result<IReadOnlyList<StockLotDto>>>;
