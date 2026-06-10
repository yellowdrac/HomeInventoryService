using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Movements.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Movements.Queries.GetMovements;

public sealed class GetMovementsQueryHandler
    : IRequestHandler<GetMovementsQuery, Result<PagedResult<MovementDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetMovementsQueryHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _currentUser = currentUser;
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result<PagedResult<MovementDto>>> Handle(
        GetMovementsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<PagedResult<MovementDto>>(HouseholdErrors.NoHousehold);
        }

        var query = _context.Movements.Where(m => m.HouseholdId == householdId);

        if (request.ItemId is { } itemId)
        {
            query = query.Where(m => m.ItemId == itemId);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(m => m.FromLocationId == locationId || m.ToLocationId == locationId);
        }

        if (request.Type is { } type)
        {
            query = query.Where(m => m.Type == type);
        }

        if (request.DateFrom is { } dateFrom)
        {
            query = query.Where(m => m.OccurredAt >= dateFrom);
        }

        if (request.DateTo is { } dateTo)
        {
            query = query.Where(m => m.OccurredAt <= dateTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var itemIds = movements.Select(m => m.ItemId).Distinct().ToList();
        var itemsById = await _context.Items
            .Where(i => i.HouseholdId == householdId && itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

        var displayNamesByUserId = new Dictionary<Guid, string>();
        foreach (var userId in movements.Select(m => m.PerformedByUserId).Distinct())
        {
            var user = await _identityService.FindByIdAsync(userId, cancellationToken);
            displayNamesByUserId[userId] = user?.DisplayName ?? string.Empty;
        }

        var dtos = movements
            .Select(m => new MovementDto(
                m.Id,
                m.ItemId,
                itemsById.TryGetValue(m.ItemId, out var itemName) ? itemName : string.Empty,
                m.FromLocationId,
                ResolveLocationName(m.FromLocationId, locationsById),
                m.ToLocationId,
                ResolveLocationName(m.ToLocationId, locationsById),
                m.Quantity,
                m.Type,
                m.Reason,
                m.PerformedByUserId,
                displayNamesByUserId.TryGetValue(m.PerformedByUserId, out var name) ? name : string.Empty,
                m.OccurredAt))
            .ToList();

        return new PagedResult<MovementDto>(dtos, request.Page, request.PageSize, totalCount);
    }

    private static string? ResolveLocationName(Guid? locationId, IReadOnlyDictionary<Guid, string> namesById) =>
        locationId is { } id && namesById.TryGetValue(id, out var name) ? name : null;
}
