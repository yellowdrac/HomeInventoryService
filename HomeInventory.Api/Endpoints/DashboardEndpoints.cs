using HomeInventory.Api.Extensions;
using HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("", async (
            DateOnly? asOfDate,
            int? withinDays,
            int? recentMovementsCount,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new GetDashboardSummaryQuery(asOfDate, withinDays ?? 7, recentMovementsCount ?? 5),
                ct)).ToHttpResult());

        return app;
    }
}
