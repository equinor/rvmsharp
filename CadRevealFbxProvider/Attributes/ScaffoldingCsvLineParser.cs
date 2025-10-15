namespace CadRevealFbxProvider.Attributes;

using Csv;

public static class ScaffoldingCsvLineParser
{
    /// <summary>
    /// Extracts the value for a given column header from a CSV row, ignoring case.
    /// Throws if there is not exactly one non-empty value for the header.
    /// </summary>
    public static string ExtractColumnValueFromRow(string columnHeader, ICsvLine row)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el => el.header.Equals(columnHeader, StringComparison.OrdinalIgnoreCase))
            .Select(el => row[el.header])
            .Single(x => !string.IsNullOrWhiteSpace(x));
    }

    /// <summary>
    /// Extracts the single non-empty weight value from a CSV row.
    /// Handles columns named "weight" or "vekt" (case-insensitive).
    /// Returns null if no such value exists.
    /// Throws if there is more than one non-empty value.
    /// </summary>
    public static string? ExtractSingleWeightFromCsvRow(ICsvLine row)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el =>
                el.header.Contains("weight", StringComparison.OrdinalIgnoreCase)
                || el.header.Contains("vekt", StringComparison.OrdinalIgnoreCase)
            )
            .Select(el => row.Values[el.index])
            .Distinct()
            .SingleOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    /// <summary>
    /// Extracts the single non-empty description value from a CSV row.
    /// If manufacturerColumnPresent is false, prefixes the description with the manufacturer name from the header.
    /// Throws if there is not exactly one non-empty description value.
    /// </summary>
    public static string ExtractSingleDescriptionFromCsvRow(ICsvLine row, bool manufacturerColumnPresent)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el => el.header.Contains("description", StringComparison.OrdinalIgnoreCase))
            .Select(el =>
            {
                var partDescription = row.Values[el.index];
                if (!manufacturerColumnPresent)
                {
                    var manufacturerName = el.header.ToUpper().Replace("DESCRIPTION", string.Empty).Trim();
                    var spacer = (manufacturerName.Length > 0) ? " " : "";
                    return partDescription.Length > 0 ? $"{manufacturerName}{spacer}{partDescription}" : string.Empty;
                }
                return partDescription.Length > 0 ? $"{partDescription}" : string.Empty;
            })
            .Distinct()
            .Single(x => !string.IsNullOrWhiteSpace(x));
    }

    /// <summary>
    /// Extracts the key value from a CSV row at the specified column index.
    /// Throws ScaffoldingAttributeParsingException if the key is missing or empty.
    /// </summary>
    public static string ExtractKeyFromCsvRow(ICsvLine row, int columnIndexKey, string keyAttribute)
    {
        var key = row.Values[columnIndexKey];
        if (string.IsNullOrEmpty(key))
            throw new ScaffoldingAttributeParsingException($"Key attribute {keyAttribute} cannot have missing values.");
        return key;
    }

    /// <summary>
    /// Determines if the column at the given index in the row matches any of the provided numeric headers (case-insensitive).
    /// </summary>
    public static bool IsNumericSapColumn(ICsvLine row, int columnIndex, List<string> numericHeaders)
    {
        return numericHeaders.Any(h => string.Equals(h, row.Headers[columnIndex], StringComparison.OrdinalIgnoreCase));
    }
}
