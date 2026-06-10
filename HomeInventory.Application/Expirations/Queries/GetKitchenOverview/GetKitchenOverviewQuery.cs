using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Expirations.Common;
using MediatR;

namespace HomeInventory.Application.Expirations.Queries.GetKitchenOverview;

/// <summary>
/// Dashboard summary of perishable stock: expired and expiring-soon counts, total perishable lots and
/// the soonest expiration. <paramref name="LocationId"/> scopes to a location subtree;
/// <paramref name="AsOfDate"/> is the client's local "today" (defaults to the current UTC date).
/// </summary>
public sealed record GetKitchenOverviewQuery(
    Guid? LocationId = null,
    int WithinDays = 7,
    DateOnly? AsOfDate = null)
    : IRequest<Result<KitchenOverviewDto>>;
