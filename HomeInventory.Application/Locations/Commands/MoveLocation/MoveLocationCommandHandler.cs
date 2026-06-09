using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Commands.MoveLocation;

public sealed class MoveLocationCommandHandler
    : IRequestHandler<MoveLocationCommand, Result<LocationDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public MoveLocationCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<LocationDto>> Handle(
        MoveLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<LocationDto>(HouseholdErrors.NoHousehold);
        }

        // The household tree is small (a single house), so load it whole and reason in memory.
        var nodes = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var node = nodes.FirstOrDefault(l => l.Id == request.Id);
        if (node is null)
        {
            return Result.Failure<LocationDto>(LocationErrors.NotFound);
        }

        if (request.NewParentId is { } newParentId)
        {
            // A destination outside the household is not in the loaded set, so it is rejected here.
            var newParent = nodes.FirstOrDefault(l => l.Id == newParentId);
            if (newParent is null)
            {
                return Result.Failure<LocationDto>(LocationErrors.ParentNotFound);
            }

            if (newParentId == node.Id || IsDescendant(nodes, ancestorId: node.Id, candidateId: newParentId))
            {
                return Result.Failure<LocationDto>(LocationErrors.CycleDetected);
            }
        }

        node.ParentId = request.NewParentId;
        node.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return node.ToDto();
    }

    /// <summary>Returns true when <paramref name="candidateId"/> sits anywhere under <paramref name="ancestorId"/>.</summary>
    private static bool IsDescendant(IReadOnlyCollection<Location> nodes, Guid ancestorId, Guid candidateId)
    {
        var childrenByParent = nodes
            .Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var queue = new Queue<Guid>();
        queue.Enqueue(ancestorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (child.Id == candidateId)
                {
                    return true;
                }

                queue.Enqueue(child.Id);
            }
        }

        return false;
    }
}
