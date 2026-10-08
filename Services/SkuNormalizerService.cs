using System.Text.RegularExpressions;

namespace WareDocs.Api.Services;

public partial class SkuNormalizerService : ISkuNormalizerService
{
    public string Normalize(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return string.Empty;

        // Uppercase, strip common separators (dashes, dots, spaces, underscores)
        var normalized = StripSeparators().Replace(sku.Trim().ToUpperInvariant(), "");
        return normalized;
    }

    public (bool Same, double Confidence) Compare(string sku1, string sku2)
    {
        if (string.IsNullOrWhiteSpace(sku1) || string.IsNullOrWhiteSpace(sku2))
            return (false, 0);

        var norm1 = Normalize(sku1);
        var norm2 = Normalize(sku2);

        // Exact match after normalization
        if (norm1 == norm2)
            return (true, 1.0);

        // Levenshtein distance for fuzzy matching
        var distance = LevenshteinDistance(norm1, norm2);
        var maxLen = Math.Max(norm1.Length, norm2.Length);

        if (maxLen == 0)
            return (true, 1.0);

        var similarity = 1.0 - ((double)distance / maxLen);

        // Threshold: if > 85% similar, consider the same
        var isSame = similarity >= 0.85;
        var confidence = Math.Round(similarity, 2);

        return (isSame, confidence);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    [GeneratedRegex(@"[\-\.\s_]")]
    private static partial Regex StripSeparators();
}
