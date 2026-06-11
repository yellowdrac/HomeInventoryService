using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using MediatR;

namespace HomeInventory.Application.Locations.Queries.GetPrintableLocations;

/// <summary>
/// Returns a flat list of the household's locations (id, name, breadcrumb and slug) to build a
/// printable sheet of QR labels. When <paramref name="LocationId"/> is set, the list is scoped to
/// that location and all of its descendants.
/// </summary>
public sealed record GetPrintableLocationsQuery(Guid? LocationId = null)
    : IRequest<Result<IReadOnlyList<PrintableLocationDto>>>;
