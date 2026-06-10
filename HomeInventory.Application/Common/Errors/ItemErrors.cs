using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the item flows.</summary>
public static class ItemErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Item.NotFound", "The item was not found in your household.");

    public static readonly Error DuplicateName =
        Error.Conflict(
            "Item.DuplicateName",
            "An item with the same name already exists in your household.");

    public static readonly Error HasStock =
        Error.Conflict(
            "Item.HasStock",
            "The item still has stock. Remove its stock lots before deleting it.");
}
