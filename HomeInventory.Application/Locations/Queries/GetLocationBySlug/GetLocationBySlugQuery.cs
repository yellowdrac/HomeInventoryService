using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;

namespace HomeInventory.Application.Locations.Queries.GetLocationBySlug;

/// <summary>
/// Resolves a QR slug to a location within the current household and returns its detail
/// (breadcrumb and children) plus its contents. Fails with "not found" when the slug does
/// not belong to the user's household.
/// </summary>
public sealed record GetLocationBySlugQuery(string QrSlug)
    : IRequest<Result<LocationBySlugDto>>;
