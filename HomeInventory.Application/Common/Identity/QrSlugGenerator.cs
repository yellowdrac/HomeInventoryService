using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HomeInventory.Application.Common.Abstractions;

namespace HomeInventory.Application.Common.Identity;

/// <summary>
/// Builds a slug from the location name (diacritics stripped, lower-cased, non-alphanumeric
/// collapsed into single hyphens) followed by a short random suffix from an unambiguous alphabet.
/// The suffix makes the slug unique while keeping it readable on a printed QR label.
/// </summary>
public sealed class QrSlugGenerator : IQrSlugGenerator
{
    private const string SuffixAlphabet = "abcdefghijkmnpqrstuvwxyz23456789";
    private const int SuffixLength = 6;
    private const int MaxBaseLength = 40;

    public string Generate(string name)
    {
        var slugBase = Slugify(name);
        var suffix = RandomSuffix();
        return slugBase.Length == 0 ? suffix : $"{slugBase}-{suffix}";
    }

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasHyphen = false;

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastWasHyphen = false;
            }
            else if (builder.Length > 0 && !lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }

            if (builder.Length >= MaxBaseLength)
            {
                break;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string RandomSuffix()
    {
        Span<char> buffer = stackalloc char[SuffixLength];
        for (var i = 0; i < SuffixLength; i++)
        {
            buffer[i] = SuffixAlphabet[RandomNumberGenerator.GetInt32(SuffixAlphabet.Length)];
        }

        return new string(buffer);
    }
}
