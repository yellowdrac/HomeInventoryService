namespace HomeInventory.Infrastructure.Identity;

/// <summary>Names of the custom JWT claims shared between token creation and reading.</summary>
public static class AppClaims
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string HouseholdId = "householdId";

    /// <summary>Unix timestamp (seconds) at which the entire login session expires.</summary>
    public const string SessionExp = "sessionExp";
}
