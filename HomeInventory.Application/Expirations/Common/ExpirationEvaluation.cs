namespace HomeInventory.Application.Expirations.Common;

/// <summary>
/// Pure expiry math shared by the expiration queries: days until expiry (negative once expired) and
/// the <see cref="ExpirationStatus"/> for a given "today" (<c>asOf</c>) and warning window.
/// </summary>
public static class ExpirationEvaluation
{
    /// <summary>Whole days from <paramref name="asOf"/> to <paramref name="expiration"/>; negative if expired.</summary>
    public static int DaysUntil(DateOnly expiration, DateOnly asOf) =>
        expiration.DayNumber - asOf.DayNumber;

    /// <summary>
    /// Classifies a lot: <see cref="ExpirationStatus.Expired"/> when already past, otherwise
    /// <see cref="ExpirationStatus.ExpiringSoon"/> when due within <paramref name="withinDays"/>,
    /// otherwise <see cref="ExpirationStatus.Ok"/>.
    /// </summary>
    public static ExpirationStatus GetStatus(DateOnly expiration, DateOnly asOf, int withinDays)
    {
        var days = DaysUntil(expiration, asOf);

        if (days < 0)
        {
            return ExpirationStatus.Expired;
        }

        return days <= withinDays ? ExpirationStatus.ExpiringSoon : ExpirationStatus.Ok;
    }
}
