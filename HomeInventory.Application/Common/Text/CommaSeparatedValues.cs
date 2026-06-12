namespace HomeInventory.Application.Common.Text;

/// <summary>
/// Parses a comma-separated configuration value into a clean list of entries. Handy for
/// passing a list through a single environment variable (for example the CORS allowed
/// origins in <c>Cors:AllowedOrigins</c>): entries are trimmed and blank ones are dropped.
/// </summary>
public static class CommaSeparatedValues
{
    /// <summary>
    /// Splits <paramref name="value"/> on commas, trimming each entry and discarding empty
    /// or whitespace-only ones. Returns an empty array when <paramref name="value"/> is
    /// <see langword="null"/>, empty or whitespace.
    /// </summary>
    public static string[] Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
