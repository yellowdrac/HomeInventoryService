using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Expirations.Common;
using MediatR;

namespace HomeInventory.Application.Expirations.Queries.GetExpiringStock;

/// <summary>
/// Lists perishable stock lots (those with an <c>ExpirationDate</c>) due on or before
/// <c>asOf + withinDays</c>, ordered FEFO (earliest expiry first). Already-expired lots are included
/// when <paramref name="IncludeExpired"/> is true. <paramref name="LocationId"/> scopes the result to
/// that location and its whole subtree; <paramref name="AsOfDate"/> is the client's local "today"
/// (defaults to the current UTC date).
/// </summary>
public sealed record GetExpiringStockQuery(
    int WithinDays = 7,
    bool IncludeExpired = true,
    Guid? LocationId = null,
    string? Category = null,
    DateOnly? AsOfDate = null)
    : IRequest<Result<IReadOnlyList<ExpiringLotDto>>>;
