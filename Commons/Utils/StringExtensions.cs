namespace Commons.Utils;

using System;
using System.Linq;

public static class StringExtensions
{
    /// <summary>
    /// Extends the string class by providing a method that checks for exact matches.
    /// </summary>
    /// <param name="str">The string to be checked.</param>
    /// <param name="keywordList">A list of strings to compare against.</param>
    /// <param name="comparisonType">Specifies whether the comparison is case-sensitive (StringComparison.Ordinal to make case sensitive or StringComparison.OrdinalIgnoreCase for case insensitive).</param>
    /// <returns>True if the target string contains an exact match to any string in the list; otherwise, false.</returns>
    public static bool ContainsAny(
        this string str,
        string[] keywordList,
        StringComparison comparisonType = StringComparison.Ordinal
    ) => keywordList.Any(s => str.Contains(s, comparisonType));
}
