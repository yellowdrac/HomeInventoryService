using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;

namespace HomeInventory.Application.Locations.Commands.MoveLocation;

/// <summary>
/// Reassigns the parent of a location (or makes it a root when <paramref name="NewParentId"/> is null).
/// Rejects moving a node into itself or any of its descendants, and destinations of another household.
/// </summary>
public sealed record MoveLocationCommand(Guid Id, Guid? NewParentId)
    : IRequest<Result<LocationDto>>;
