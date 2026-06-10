namespace HomeInventory.Application.Common.Text;

/// <summary>
/// In-memory, PostgreSQL <c>pg_trgm</c>-compatible trigram similarity. Mirrors the
/// <c>similarity()</c> function (Jaccard index over the trigram sets) so fuzzy search tolerates
/// typos (e.g. "duracel" matches "duracell"). Inputs are expected to already be normalized with
/// <see cref="TextNormalization"/> (lower-case, accent-stripped).
/// </summary>
public static class TrigramSimilarity
{
    /// <summary>
    /// Default match cut-off, matching the <c>pg_trgm</c> default <c>similarity_threshold</c> (0.3).
    /// </summary>
    public const double DefaultThreshold = 0.3;

    /// <summary>
    /// Similarity of <paramref name="a"/> and <paramref name="b"/> in the range [0, 1]: the number of
    /// shared trigrams divided by the number of distinct trigrams across both strings.
    /// </summary>
    public static double Compute(string a, string b)
    {
        var first = GenerateTrigrams(a);
        var second = GenerateTrigrams(b);

        if (first.Count == 0 && second.Count == 0)
        {
            return 0d;
        }

        var intersection = 0;
        foreach (var trigram in first)
        {
            if (second.Contains(trigram))
            {
                intersection++;
            }
        }

        var union = first.Count + second.Count - intersection;
        return union == 0 ? 0d : (double)intersection / union;
    }

    /// <summary>
    /// Builds the set of trigrams for <paramref name="value"/> the way <c>pg_trgm</c> does: each
    /// maximal run of alphanumeric characters is padded with two leading spaces and one trailing
    /// space, then sliced into three-character windows.
    /// </summary>
    private static HashSet<string> GenerateTrigrams(string value)
    {
        var trigrams = new HashSet<string>();

        var start = 0;
        while (start < value.Length)
        {
            if (!char.IsLetterOrDigit(value[start]))
            {
                start++;
                continue;
            }

            var end = start;
            while (end < value.Length && char.IsLetterOrDigit(value[end]))
            {
                end++;
            }

            AddWordTrigrams(value.AsSpan(start, end - start), trigrams);
            start = end;
        }

        return trigrams;
    }

    private static void AddWordTrigrams(ReadOnlySpan<char> word, HashSet<string> trigrams)
    {
        // Pad with two leading and one trailing space, e.g. "word" -> "  word ".
        Span<char> padded = stackalloc char[word.Length + 3];
        padded[0] = ' ';
        padded[1] = ' ';
        word.CopyTo(padded[2..]);
        padded[^1] = ' ';

        for (var i = 0; i + 3 <= padded.Length; i++)
        {
            trigrams.Add(padded.Slice(i, 3).ToString());
        }
    }
}
