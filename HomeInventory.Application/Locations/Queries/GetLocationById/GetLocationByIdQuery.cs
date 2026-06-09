using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;

namespace HomeInventory.Application.Locations.Queries.GetLocationById;

/// <summary>Returns a node with its breadcrumb (root → node) and its direct children.</summary>
public sealed record GetLocationByIdQuery(Guid Id)
    : IRequest<Result<LocationDetailDto>>;
