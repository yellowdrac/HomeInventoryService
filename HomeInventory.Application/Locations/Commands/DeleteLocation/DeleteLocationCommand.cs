using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Locations.Commands.DeleteLocation;

/// <summary>
/// Deletes a location. Rejected when the node has children or still holds stock lots;
/// the caller must empty or move those first. There is no cascade delete.
/// </summary>
public sealed record DeleteLocationCommand(Guid Id) : IRequest<Result>;
