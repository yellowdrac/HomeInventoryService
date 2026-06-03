using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the household flows.</summary>
public static class HouseholdErrors
{
    public static readonly Error AlreadyInHousehold =
        Error.Conflict("Household.AlreadyInHousehold", "The user already belongs to a household.");

    public static readonly Error InvalidJoinCode =
        Error.NotFound("Household.InvalidJoinCode", "No household matches the provided join code.");

    public static readonly Error NoHousehold =
        Error.NotFound("Household.NoHousehold", "The current user does not belong to a household.");

    public static readonly Error NotOwner =
        Error.Forbidden("Household.NotOwner", "Only the household owner can perform this action.");

    public static readonly Error UserNotFound =
        Error.NotFound("Household.UserNotFound", "The current user could not be found.");
}
