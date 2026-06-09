using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Queries.GetLocationById;

public sealed class GetLocationByIdQueryHandler
    : IRequestHandler<GetLocationByIdQuery, Result<LocationDetailDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public GetLocationByIdQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<LocationDetailDto>> Handle(
        GetLocationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<LocationDetailDto>(HouseholdErrors.NoHousehold);
        }

        // Small dataset: load the household nodes once and derive breadcrumb/children in memory.
        var nodes = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var node = nodes.FirstOrDefault(l => l.Id == request.Id);
        if (node is null)
        {
            return Result.Failure<LocationDetailDto>(LocationErrors.NotFound);
        }

        var byId = nodes.ToDictionary(n => n.Id);

        // Walk up to the root, then reverse so the breadcrumb reads root → node (inclusive).
        var breadcrumb = new List<LocationDto>();
        var current = node;
        while (current is not null)
        {
            breadcrumb.Add(current.ToDto());
            current = current.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent)
                ? parent
                : null;
        }

        breadcrumb.Reverse();

        var children = nodes
            .Where(l => l.ParentId == node.Id)
            .OrderBy(l => l.Name)
            .Select(l => l.ToDto())
            .ToList();

        return new LocationDetailDto(
            node.Id,
            node.Name,
            node.Type,
            node.ParentId,
            node.QrSlug,
            breadcrumb,
            children);
    }
}
