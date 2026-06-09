using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the location flows.</summary>
public static class LocationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Location.NotFound", "The location was not found in your household.");

    public static readonly Error ParentNotFound =
        Error.NotFound("Location.ParentNotFound", "The parent location was not found in your household.");

    public static readonly Error CycleDetected =
        Error.Conflict(
            "Location.CycleDetected",
            "A location cannot be moved into itself or one of its own descendants.");

    public static readonly Error HasChildren =
        Error.Conflict(
            "Location.HasChildren",
            "The location has child locations. Move or delete them before deleting it.");

    public static readonly Error HasStockLots =
        Error.Conflict(
            "Location.HasStockLots",
            "The location still holds stock. Empty or move its stock before deleting it.");
}
