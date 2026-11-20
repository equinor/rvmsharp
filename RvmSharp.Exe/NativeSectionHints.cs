namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Structural profile family named by a section's catalogue reference.</summary>
internal enum ProfileFamily : byte
{
    /// <summary>Not under a section node.</summary>
    None = 0,

    /// <summary>A section whose catalogue reference names no known profile.</summary>
    Any,
    L,
    C,
    IH,
    T,
    Hollow,
    Solid,
}

/// <summary>Reads the section profile of a node from its PDMS attributes (<c>Type</c> and <c>Spref</c>).</summary>
internal static partial class NativeSectionHints
{
    private static readonly HashSet<string> SectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SCTN",
        "GENSEC",
        "FITT",
        "FIXING",
        "JLDATU",
        "PLDATU",
    };

    /// <summary>The node's catalogue reference when it is a section, otherwise null.</summary>
    internal static string? SectionReference(IReadOnlyDictionary<string, string> attributes) =>
        attributes.TryGetValue("Type", out var type)
        && SectionTypes.Contains(type.Trim())
        && attributes.TryGetValue("Spref", out var spref)
        && !string.IsNullOrWhiteSpace(spref)
            ? spref.Trim()
            : null;

    /// <summary>Profile family from a reference such as <c>/GB/T11263-HW150X150X7X10-Q235B</c>.</summary>
    internal static ProfileFamily Family(string? spref)
    {
        if (spref == null)
            return ProfileFamily.None;
        foreach (Match match in ProfileToken().Matches(spref))
        {
            var prefix = match.Groups["prefix"].Value.ToUpperInvariant();
            // A one-letter prefix with a bare number is usually a document or standard number
            // (GB/T11263); multi-letter designations such as IPE200 or UPN200 are profiles.
            if (match.Groups["dims"].Length == 0 && prefix.Length == 1)
                continue;
            return prefix switch
            {
                "L" or "UA" => ProfileFamily.L,
                "UPN" or "UPE" or "PFC" or "MC" or "U" or "C" => ProfileFamily.C,
                "WT" or "T" => ProfileFamily.T,
                "TS" or "RHS" or "SHS" or "CHS" or "HSS" or "HS" or "PIPE" => ProfileFamily.Hollow,
                "FB" or "FL" or "PL" or "RB" or "RD" or "BAR" => ProfileFamily.Solid,
                _ => ProfileFamily.IH,
            };
        }
        return ProfileFamily.Any;
    }

    [GeneratedRegex(
        @"(?<![A-Z0-9])(?<prefix>HEA|HEB|HEM|HE|HW|HN|HM|HP|HSS|HS|IPE|IPN|UB|UC|UPN|UPE|UA|PFC|MC|WT|TS|RHS|SHS|CHS|PIPE|FB|FL|PL|RB|RD|BAR|W|H|I|L|U|C|T)\d+(?:[.,]\d+)?(?<dims>(?:X\d+(?:[.,]\d+)?)*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex ProfileToken();
}
