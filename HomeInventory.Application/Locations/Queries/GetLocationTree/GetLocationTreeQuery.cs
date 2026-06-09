using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;

namespace HomeInventory.Application.Locations.Queries.GetLocationTree;

/// <summary>Returns the household location forest (every root with its nested children).</summary>
public sealed record GetLocationTreeQuery
    : IRequest<Result<IReadOnlyList<LocationTreeNodeDto>>>;
