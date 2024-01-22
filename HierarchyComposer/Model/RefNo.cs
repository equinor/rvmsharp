namespace HierarchyComposer.Model;

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

/// <summary>
/// In the PDMS format every element has an ID, which is unique to an object.
/// Some parts do not have their own Ids but are attached to a part, they are identified as ILTUBOF See remarks..
/// It consists of three parts.
/// 1. OPTIONAL: Prefix of a single word. Such as ILTUBOF
/// 2. Required: A DatabaseNumber
/// 3. Required: A SequenceNumber
/// It will look like this in the data: "=123/456" or "ILTUBOF=123/456"
/// </summary>
/// <remarks>
/// Prefixes such as ILTUBOF mean Implicit Leaving TUBi OF =RefNoDb/RefNoSequence, and indicates that this is
/// and implicitly connecting tube that connects from the previous part (such as a bend or flange), to the next part (bend or flange etc.).
/// </remarks>
[DebuggerDisplay("{Prefix}={DbNo}/{SequenceNo}")]
public partial class RefNo
{
    /// <summary>
    /// Prefix: In the current known data this is either empty or ILTUBOF
    /// Its really a "Query" for the data, in PDMS. But we do not support queries (yet) so we store it as a "dumb" prefix.
    /// </summary>
    /// <remarks>
    /// This should never be null as it would make DB queries a lot more complicated ((NULL = NULL) is UNKNOWN in SQL)
    /// </remarks>
    public string Prefix { get; init; }
    public int DbNo { get; }
    public int SequenceNo { get; }

    public override string ToString()
    {
        return $"{Prefix}={DbNo}/{SequenceNo}";
    }

    public RefNo(string? prefix, int dbNo, int sequenceNo)
    {
        if (!string.IsNullOrEmpty(prefix) && !PrefixValidRegex.IsMatch(prefix))
        {
            throw new ArgumentException(
                $"Prefix \"{prefix}\" is unexpected, is this a valid prefix? If so update the code and tests."
            );
        }

        Prefix = prefix ?? string.Empty;
        DbNo = dbNo;
        SequenceNo = sequenceNo;
    }

    private static readonly Regex RefNoRegex = RefNoMatchingRegex();

    // Ensure the regex is only
    private static readonly Regex PrefixValidRegex = new Regex("^\\w+$", RegexOptions.Compiled);

    /// <summary>
    /// Parse a RefNo-string into a RefNo
    /// </summary>
    /// <param name="refNo">Expects a "=123/456" or "PREFIX=123/321" formatted string</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">If the input format is not correct</exception>
    /// <exception cref="ArgumentNullException"></exception>
    [Pure]
    public static RefNo Parse(string refNo)
    {
        ArgumentNullException.ThrowIfNull(refNo);

        var match = RefNoRegex.Match(refNo.Trim());

        if (!match.Success)
            throw new ArgumentException(
                $"Expected format 'prefix=123/321' '(string?=uint/uint)' (prefix is optional), was '{refNo}'",
                nameof(refNo)
            );

        // Regex Group 0 is the entire match.
        var prefixParsed = match.Groups[1].Value;
        var dbNo = int.Parse(match.Groups[2].Value);
        var sequenceNo = int.Parse(match.Groups[3].Value);

        if (dbNo < 0 || sequenceNo < 0)
            throw new ArgumentException($"Expected positive values, was '{refNo}'", nameof(refNo));

        // Save empty prefixes as string.empty
        var prefix = string.IsNullOrWhiteSpace(prefixParsed) ? String.Empty : prefixParsed;

        return new RefNo(prefix, dbNo, sequenceNo);
    }

    /// <summary>
    /// Matches strings with this pattern: PREFIX?=123/456, with one capturing group () for each part.
    /// Prefix is optional
    /// </summary>
    [GeneratedRegex("^(\\w+)?=(\\d+)\\/(\\d+)$", RegexOptions.Compiled)]
    private static partial Regex RefNoMatchingRegex();
}
