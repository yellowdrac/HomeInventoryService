using System.Globalization;
using System.Text;

namespace HomeInventory.Application.Common.Text;

/// <summary>
/// Produces a search-friendly form of a name: trimmed, lower-cased, accent-stripped and
/// whitespace-collapsed. Used to compute and to query the item <c>NormalizedName</c> key,
/// so search is insensitive to case and accents.
/// </summary>
public static class TextNormalization
{
    public static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
            lastWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }
}
