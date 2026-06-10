using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Movements.Common;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Application.Movements.Queries.GetMovements;

/// <summary>
/// Returns a page of movements ordered by <c>OccurredAt</c> descending (newest first). Optional
/// filters: by item, by location (matched on either the source or the destination), by type and by
/// an inclusive <c>OccurredAt</c> date range.
/// </summary>
public sealed record GetMovementsQuery(
    Guid? ItemId = null,
    Guid? LocationId = null,
    MovementType? Type = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<MovementDto>>>;
