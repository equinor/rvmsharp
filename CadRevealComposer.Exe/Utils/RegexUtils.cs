namespace CadRevealComposer.Exe;

using System;
using System.Text.RegularExpressions;

public static class RegexUtils
{
    /// <summary>
    /// Check if the input regex is a valid regex.
    /// From: https://stackoverflow.com/a/1775017
    /// </summary>
    public static bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            Regex.Match("", pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
}
