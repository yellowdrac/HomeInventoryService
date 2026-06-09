using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Enums;
using MediatR;

namespace HomeInventory.Application.Locations.Commands.UpdateLocation;

/// <summary>Renames a location and/or changes its type. The parent is left untouched.</summary>
public sealed record UpdateLocationCommand(Guid Id, string Name, LocationType Type)
    : IRequest<Result<LocationDto>>;
