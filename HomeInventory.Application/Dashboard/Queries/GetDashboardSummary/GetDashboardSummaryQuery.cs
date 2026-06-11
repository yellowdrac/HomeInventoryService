using HomeInventory.Application.Dashboard.Common;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Home overview scoped to the current household. <paramref name="AsOfDate"/> is the client's local
/// "today" used for the expiration counts (defaults to the current UTC date);
/// <paramref name="WithinDays"/> is the expiring-soon warning window and
/// <paramref name="RecentMovementsCount"/> caps how many recent movements are returned.
/// </summary>
public sealed record GetDashboardSummaryQuery(
    DateOnly? AsOfDate = null,
    int WithinDays = 7,
    int RecentMovementsCount = 5)
    : IRequest<Result<DashboardSummaryDto>>;
