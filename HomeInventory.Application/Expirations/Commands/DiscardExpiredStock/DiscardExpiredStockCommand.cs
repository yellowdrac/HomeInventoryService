using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Expirations.Commands.DiscardExpiredStock;

/// <summary>
/// Discards every expired stock lot in one transaction, recording a <c>Discarded</c> movement per lot.
/// <paramref name="LocationId"/> scopes to a location subtree; <paramref name="AsOfDate"/> is the
/// client's local "today" (defaults to the current UTC date). Returns the number of lots discarded.
/// </summary>
public sealed record DiscardExpiredStockCommand(
    Guid? LocationId = null,
    DateOnly? AsOfDate = null)
    : IRequest<Result<int>>;
