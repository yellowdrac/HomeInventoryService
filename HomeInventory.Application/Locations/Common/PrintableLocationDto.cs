namespace HomeInventory.Application.Locations.Common;

/// <summary>
/// Flat read model used to build a printable sheet of QR labels: a location with its name,
/// its breadcrumb path (root → location, inclusive) and the slug that the front encodes into a QR.
/// </summary>
public sealed record PrintableLocationDto(
    Guid Id,
    string Name,
    string Breadcrumb,
    string QrSlug);
