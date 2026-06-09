using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Queries.GetLocationTree;

public sealed class GetLocationTreeQueryHandler
    : IRequestHandler<GetLocationTreeQuery, Result<IReadOnlyList<LocationTreeNodeDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetLocationTreeQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<IReadOnlyList<LocationTreeNodeDto>>> Handle(
        GetLocationTreeQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<IReadOnlyList<LocationTreeNodeDto>>(HouseholdErrors.NoHousehold);
        }

        // The volume is small (one house), so load every node and assemble the tree in memory.
        var nodes = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var childrenByParent = nodes
            .Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        LocationTreeNodeDto Build(Location node) =>
            new(
                node.Id,
                node.Name,
                node.Type,
                node.ParentId,
                node.QrSlug,
                childrenByParent.TryGetValue(node.Id, out var children)
                    ? children.OrderBy(c => c.Name).Select(Build).ToList()
                    : Array.Empty<LocationTreeNodeDto>());

        var roots = nodes
            .Where(n => n.ParentId is null)
            .OrderBy(n => n.Name)
            .Select(Build)
            .ToList();

        return roots;
    }
}
