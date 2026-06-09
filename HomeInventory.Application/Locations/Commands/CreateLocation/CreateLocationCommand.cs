using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Application.Locations.Commands.CreateLocation;

/// <summary>
/// Creates a location under <paramref name="ParentId"/>, or as a root when it is null.
/// Generates a household-unique <c>QrSlug</c> for the new node.
/// </summary>
public sealed record CreateLocationCommand(string Name, LocationType Type, Guid? ParentId)
    : IRequest<Result<LocationDto>>;
