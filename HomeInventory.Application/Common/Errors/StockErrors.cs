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

    public static readonly Error InsufficientQuantity =
        Error.Validation(
            "Stock.InsufficientQuantity",
            "The requested quantity exceeds the quantity available in the lot.");

    public static readonly Error SameLocation =
        Error.Validation(
            "Stock.SameLocation",
            "The destination location must be different from the current location of the lot.");

    public static readonly Error UniqueMustMoveWholeLot =
        Error.Validation(
            "Stock.UniqueMustMoveWholeLot",
            "A unique-tracked item must be moved as a whole lot.");
}
