using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Items.Queries.GetItemById;

/// <summary>Returns an item together with its stock lots (each with location name and breadcrumb).</summary>
public sealed record GetItemByIdQuery(Guid Id) : IRequest<Result<ItemDetailDto>>;
