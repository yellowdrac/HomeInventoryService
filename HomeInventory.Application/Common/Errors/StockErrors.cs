using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the stock flows.</summary>
public static class StockErrors
{
    public static readonly Error LotNotFound =
        Error.NotFound("Stock.LotNotFound", "The stock lot was not found in your household.");

    public static readonly Error ItemNotFound =
        Error.NotFound("Stock.ItemNotFound", "The referenced item was not found in your household.");

    public static readonly Error LocationNotFound =
        Error.NotFound("Stock.LocationNotFound", "The referenced location was not found in your household.");

    public static readonly Error UniqueAlreadyStocked =
        Error.Conflict(
            "Stock.UniqueAlreadyStocked",
            "A unique-tracked item can only have a single stock lot.");
}
