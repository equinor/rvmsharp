namespace Commons.Utils;

using System;
using System.Linq;

public static class StringExtensions
{
    public static bool ContainsAny(
        this string str,
        string[] keywordList,
        StringComparison comparisonType = StringComparison.Ordinal
    ) => keywordList.Any(s => str.Contains(s, comparisonType));
}
