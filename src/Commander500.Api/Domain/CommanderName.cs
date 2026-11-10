using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Commander500.Api.Domain;

public static partial class CommanderName
{
    /// <summary>Trims, collapses whitespace, unifies apostrophes, strips diacritics and lower-cases.
    /// "  Atraxa,  Praetors’ Voice " and "atraxa, praetors' voice" map to the same key.</summary>
    public static string Normalize(string name)
    {
        var s = Clean(name).Replace('’', '\'').Replace('‘', '\'').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Display form: trimmed with single spaces.</summary>
    public static string Clean(string name) => Spaces().Replace(name.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
