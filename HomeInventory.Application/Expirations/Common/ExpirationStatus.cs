namespace HomeInventory.Application.Expirations.Common;

/// <summary>
/// Expiry state of a stock lot relative to "today" and the configured warning window.
/// </summary>
public enum ExpirationStatus
{
    /// <summary>Past its expiration date.</summary>
    Expired,

    /// <summary>Not expired yet, but due within the warning window.</summary>
    ExpiringSoon,

    /// <summary>Expires beyond the warning window.</summary>
    Ok,
}
