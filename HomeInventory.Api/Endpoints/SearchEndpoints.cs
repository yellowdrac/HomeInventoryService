using HomeInventory.Api.Extensions;
using HomeInventory.Application.Items.Queries.SearchInventory;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search").RequireAuthorization();

        group.MapGet("", async (
            string q,
            string? category,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new SearchInventoryQuery(q, category, page ?? 1, pageSize ?? 20), ct)).ToHttpResult());

        return app;
    }
}
