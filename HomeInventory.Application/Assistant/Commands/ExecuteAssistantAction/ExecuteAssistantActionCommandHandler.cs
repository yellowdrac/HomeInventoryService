using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Commands.CreateItem;
using HomeInventory.Application.Locations.Commands.CreateLocation;
using HomeInventory.Application.Stock.Commands.AddStock;
using HomeInventory.Application.Stock.Commands.MoveStock;
using HomeInventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;

/// <summary>
/// Processes confirmed AI-proposed actions in order, resolving deferred entity references
/// (names that were unresolved at proposal time) through a local cache of just-created entities.
/// Re-dispatches existing write commands so all existing validations still apply.
/// </summary>
public sealed class ExecuteAssistantActionCommandHandler
    : IRequestHandler<ExecuteAssistantActionCommand, Result<ExecuteAssistantActionResult>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;
    private readonly IApplicationDbContext _context;

    public ExecuteAssistantActionCommandHandler(
        ICurrentUser currentUser,
        ISender sender,
        IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _sender = sender;
        _context = context;
    }

    public async Task<Result<ExecuteAssistantActionResult>> Handle(
        ExecuteAssistantActionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is null)
        {
            return Result.Failure<ExecuteAssistantActionResult>(HouseholdErrors.NoHousehold);
        }

        var createdEntities = new List<ExecutedEntityRef>();
        // Name → id caches for deferred reference resolution across ordered actions.
        var createdLocationsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdItemsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var action in request.Actions)
        {
            var result = await ExecuteSingleAsync(
                action,
                createdEntities,
                createdLocationsByName,
                createdItemsByName,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<ExecuteAssistantActionResult>(result.Error);
            }
        }

        return new ExecuteAssistantActionResult(createdEntities);
    }

    private Task<Result> ExecuteSingleAsync(
        ProposedAction action,
        List<ExecutedEntityRef> createdEntities,
        Dictionary<string, Guid> createdLocationsByName,
        Dictionary<string, Guid> createdItemsByName,
        CancellationToken ct) =>
        action.Type switch
        {
            ProposedActionType.CreateLocation =>
                ExecuteCreateLocationAsync(action, createdEntities, createdLocationsByName, ct),
            ProposedActionType.CreateItem =>
                ExecuteCreateItemAsync(action, createdEntities, createdItemsByName, ct),
            ProposedActionType.AddStock =>
                ExecuteAddStockAsync(action, createdEntities, createdItemsByName, createdLocationsByName, ct),
            ProposedActionType.MoveStock =>
                ExecuteMoveStockAsync(action, createdLocationsByName, ct),
            _ => Task.FromResult(Result.Success()),
        };

    private async Task<Result> ExecuteCreateLocationAsync(
        ProposedAction action,
        List<ExecutedEntityRef> createdEntities,
        Dictionary<string, Guid> createdLocationsByName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(action.LocationName))
        {
            return Result.Failure(AssistantErrors.InvalidAction);
        }

        if (!Enum.TryParse<LocationType>(action.LocationTypeName, ignoreCase: true, out var locationType))
        {
            locationType = LocationType.Room;
        }

        // The parent might have been created in a preceding step.
        var parentId = action.ParentLocationId;
        if (parentId is null && action.ParentLocationName is not null)
        {
            createdLocationsByName.TryGetValue(action.ParentLocationName, out var cachedParentId);
            parentId = cachedParentId == default ? null : cachedParentId;
        }

        var cmd = new CreateLocationCommand(action.LocationName, locationType, parentId);
        var result = await _sender.Send(cmd, ct);
        if (result.IsFailure) return Result.Failure(result.Error);

        createdEntities.Add(new ExecutedEntityRef(AssistantReferenceKind.Location, result.Value.Id, result.Value.Name));
        createdLocationsByName[result.Value.Name] = result.Value.Id;
        return Result.Success();
    }

    private async Task<Result> ExecuteCreateItemAsync(
        ProposedAction action,
        List<ExecutedEntityRef> createdEntities,
        Dictionary<string, Guid> createdItemsByName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(action.ItemName))
        {
            return Result.Failure(AssistantErrors.InvalidAction);
        }

        if (!Enum.TryParse<TrackingType>(action.ItemTrackingTypeName, ignoreCase: true, out var trackingType))
        {
            trackingType = TrackingType.Quantity;
        }

        var cmd = new CreateItemCommand(
            action.ItemName,
            action.ItemCategory,
            null,
            trackingType,
            action.ItemUnit);

        var result = await _sender.Send(cmd, ct);
        if (result.IsFailure) return Result.Failure(result.Error);

        createdEntities.Add(new ExecutedEntityRef(AssistantReferenceKind.Item, result.Value.Id, result.Value.Name));
        createdItemsByName[result.Value.Name] = result.Value.Id;
        return Result.Success();
    }

    private async Task<Result> ExecuteAddStockAsync(
        ProposedAction action,
        List<ExecutedEntityRef> createdEntities,
        Dictionary<string, Guid> createdItemsByName,
        Dictionary<string, Guid> createdLocationsByName,
        CancellationToken ct)
    {
        // Resolve item id: either directly provided or from a preceding CreateItem step.
        var itemId = action.ResolvedItemId;
        if (itemId is null && action.UnresolvedItemName is not null)
        {
            if (!createdItemsByName.TryGetValue(action.UnresolvedItemName, out var cachedItemId))
            {
                return Result.Failure(AssistantErrors.InvalidAction);
            }

            itemId = cachedItemId;
        }

        if (itemId is null) return Result.Failure(AssistantErrors.InvalidAction);

        // Resolve location id.
        var locationId = action.ResolvedLocationId;
        if (locationId is null && action.UnresolvedLocationName is not null)
        {
            if (!createdLocationsByName.TryGetValue(action.UnresolvedLocationName, out var cachedLocId))
            {
                return Result.Failure(AssistantErrors.InvalidAction);
            }

            locationId = cachedLocId;
        }

        if (locationId is null) return Result.Failure(AssistantErrors.InvalidAction);

        if (action.Quantity is not { } qty || qty <= 0)
        {
            return Result.Failure(AssistantErrors.InvalidAction);
        }

        var cmd = new AddStockCommand(itemId.Value, locationId.Value, qty, action.ExpirationDate, null);
        var result = await _sender.Send(cmd, ct);
        return result.IsFailure ? Result.Failure(result.Error) : Result.Success();
    }

    private async Task<Result> ExecuteMoveStockAsync(
        ProposedAction action,
        Dictionary<string, Guid> createdLocationsByName,
        CancellationToken ct)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var itemId = action.ResolvedItemId;
        var fromLocationId = action.ResolvedFromLocationId;

        var toLocationId = action.ResolvedToLocationId;
        if (toLocationId is null && action.UnresolvedToLocationName is not null)
        {
            if (!createdLocationsByName.TryGetValue(action.UnresolvedToLocationName, out var cachedToId))
            {
                return Result.Failure(AssistantErrors.InvalidAction);
            }

            toLocationId = cachedToId;
        }

        if (itemId is null || fromLocationId is null || toLocationId is null || action.Quantity is null)
        {
            return Result.Failure(AssistantErrors.InvalidAction);
        }

        // Find the stock lot for this item at the source location (FEFO order).
        var lot = await _context.StockLots
            .Where(s =>
                s.HouseholdId == householdId
                && s.ItemId == itemId.Value
                && s.LocationId == fromLocationId.Value)
            .OrderBy(s => s.ExpirationDate.HasValue ? 0 : 1)
            .ThenBy(s => s.ExpirationDate)
            .FirstOrDefaultAsync(ct);

        if (lot is null) return Result.Failure(StockErrors.LotNotFound);

        var cmd = new MoveStockCommand(lot.Id, toLocationId.Value, action.Quantity.Value);
        var result = await _sender.Send(cmd, ct);
        return result.IsFailure ? Result.Failure(result.Error) : Result.Success();
    }
}
