namespace CadRevealComposer.Utils;

using System;

public static class TeamCityServiceMessages
{
    /// <summary>
    /// Writes a TeamCity <c>setParameter</c> service message to standard output. For use within TeamCity.
    /// </summary>
    /// <param name="name">The unescaped build parameter name.</param>
    /// <param name="value">The unescaped literal value to assign.</param>
    /// <remarks>
    /// Escapes both attributes for the service message and doubles percent signs in the value
    /// so TeamCity treats <c>%parameter%</c> text literally instead of expanding it.
    /// </remarks>
    public static void SetParameter(string name, string value)
    {
        // TeamCity resolves %parameter% references in build parameter values after decoding the service message.
        var escapedValue = Escape(value.Replace("%", "%%"));
        Console.WriteLine($"##teamcity[setParameter name='{Escape(name)}' value='{escapedValue}']");
    }

    private static string Escape(string value)
    {
        return value
            .Replace("|", "||")
            .Replace("'", "|'")
            .Replace("\n", "|n")
            .Replace("\r", "|r")
            .Replace("[", "|[")
            .Replace("]", "|]");
    }
}
