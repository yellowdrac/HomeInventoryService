using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Queries.GetPrintableLocations;

public sealed class GetPrintableLocationsQueryHandler
    : IRequestHandler<GetPrintableLocationsQuery, Result<IReadOnlyList<PrintableLocationDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetPrintableLocationsQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<IReadOnlyList<PrintableLocationDto>>> Handle(
        GetPrintableLocationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<IReadOnlyList<PrintableLocationDto>>(HouseholdErrors.NoHousehold);
        }

        // Small dataset: load the whole household tree once and slice/label it in memory.
        var nodes = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var byId = nodes.ToDictionary(n => n.Id);

        // Optionally scope to a subtree (the given location and all of its descendants).
        IEnumerable<Location> selected = nodes;
        if (request.LocationId is { } rootId)
        {
            if (!byId.ContainsKey(rootId))
            {
                return Result.Failure<IReadOnlyList<PrintableLocationDto>>(LocationErrors.NotFound);
            }

            var childrenByParent = nodes
                .Where(n => n.ParentId is not null)
                .GroupBy(n => n.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            selected = CollectSubtree(rootId, byId, childrenByParent);
        }

        var printable = selected
            .Select(node => new PrintableLocationDto(
                node.Id,
                node.Name,
                string.Join(" / ", LocationMappingExtensions.BuildBreadcrumbNames(node.Id, byId)),
                node.QrSlug))
            .OrderBy(p => p.Breadcrumb)
            .ToList();

        return printable;
    }

    private static IEnumerable<Location> CollectSubtree(
        Guid rootId,
        IReadOnlyDictionary<Guid, Location> byId,
        IReadOnlyDictionary<Guid, List<Location>> childrenByParent)
    {
        var stack = new Stack<Guid>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            yield return byId[id];

            if (childrenByParent.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                {
                    stack.Push(child.Id);
                }
            }
        }
    }
}
