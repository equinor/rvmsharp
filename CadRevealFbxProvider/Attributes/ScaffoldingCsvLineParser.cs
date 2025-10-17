namespace CadRevealFbxProvider.Attributes;

using CadRevealFbxProvider.UserFriendlyLogger;
using Csv;

public static class ScaffoldingCsvLineParser
{
    public const string ItemCodeColumnKey = "Item code";

    /// <summary>
    /// Extracts the value for a given item code from a CSV row
    /// The presence of item code is mandatory and there should be guards for its presence before executing this method.
    /// However, to make this fail-proof, this method will catch the exception and return an "UNDEFINED" if the item code is missing or ill-defined.
    /// </summary>
    public static string ExtractItemCodeFromRowAssumingItsValid(ICsvLine row)
    {
        try
        {
            return row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Equals(ItemCodeColumnKey, StringComparison.OrdinalIgnoreCase))
                .Select(el => row[el.index])
                .Single(x => !String.IsNullOrWhiteSpace(x));
        }
        catch
        {
            return "UNDEFINED";
        }
    }

    /// <summary>
    /// Extracts the key value from a CSV row at the specified column index.
    /// Throws ScaffoldingAttributeParsingException if the key is missing or empty.
    /// </summary>
    public static string ExtractKeyFromCsvRow(ICsvLine row, string keyAttribute)
    {
        var key = row[keyAttribute];
        var rowIndex = row.Index + 1;
        if (string.IsNullOrEmpty(key))
            throw new UserFriendlyLogException(
                $"CSV processing failed on row {rowIndex}, because {keyAttribute} is a key attribute and cannot have missing values.",
                new ScaffoldingAttributeParsingException($"Key attribute {keyAttribute} cannot have missing values.")
            );
        return key;
    }

    /// <summary>
    /// Extracts the value for a given column header from a CSV row, ignoring case.
    /// Throws if there is not exactly one non-empty value for the header.
    /// </summary>
    public static string ExtractColumnValueFromRow(string columnHeader, ICsvLine row)
    {
        try
        {
            return row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Equals(columnHeader, StringComparison.OrdinalIgnoreCase))
                .Select(el => row[el.index])
                .Single(x => !String.IsNullOrWhiteSpace(x));
        }
        catch (InvalidOperationException)
        {
            var rowIndex = row.Index + 1;
            var itemCode = ExtractItemCodeFromRowAssumingItsValid(row);

            throw new UserFriendlyLogException(
                $"CSV file has one or more of the following issues with the column \"{columnHeader}\": -- it is missing entirely  -- occurs multiple times with distinct values -- it has a missing value on row {rowIndex} (Item code = {itemCode})",
                new ScaffoldingAttributeParsingException(
                    $"Attribute {columnHeader} must exist and cannot have missing values."
                )
            );
        }
    }

    /// <summary>
    /// Extracts the single non-empty weight value from a CSV row.
    /// Handles columns named "weight" or "vekt" (case-insensitive).
    /// Returns null if no such value exists.
    /// Throws if there is more than one non-empty value.
    /// </summary>
    public static string? ExtractSingleWeightFromCsvRow(ICsvLine row)
    {
        try
        {
            return row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el =>
                    el.header.Contains("weight", StringComparison.OrdinalIgnoreCase)
                    || el.header.Contains("vekt", StringComparison.OrdinalIgnoreCase)
                )
                .Select(el => row[el.index])
                .Distinct()
                .SingleOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }
        catch (InvalidOperationException)
        {
            // this should not throw, as this is a column that is created programatically during processing
            var rowIndex = row.Index + 1;

            var itemCode = row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Equals(ItemCodeColumnKey, StringComparison.OrdinalIgnoreCase))
                .Select(el => row[el.index])
                .Distinct()
                .Single(x => !String.IsNullOrWhiteSpace(x));

            throw new UserFriendlyLogException(
                $"CSV on row {rowIndex} (Item Code = {itemCode}) has one of the following problems. -- Missing item weight -- More than one distinct item weights.",
                new ScaffoldingAttributeParsingException(
                    $"Weight attributes cannot have multiple values. Only one weight attribute per item is allowed."
                )
            );
        }
    }

    /// <summary>
    /// Extracts the single non-empty description value from a CSV row.
    /// If manufacturerColumnPresent is false, prefixes the description with the manufacturer name from the header.
    /// Throws if there is not exactly one non-empty description value.
    /// </summary>
    public static string ExtractSingleDescriptionFromCsvRow(ICsvLine row, bool manufacturerColumnPresent)
    {
        try
        {
            return row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Contains("description", StringComparison.OrdinalIgnoreCase))
                .Select(el =>
                {
                    var partDescription = row[el.index];
                    if (!manufacturerColumnPresent)
                    {
                        var manufacturerName = el.header.ToUpper().Replace("DESCRIPTION", string.Empty).Trim();
                        var spacer = (manufacturerName.Length > 0) ? " " : "";
                        return partDescription.Length > 0
                            ? $"{manufacturerName}{spacer}{partDescription}"
                            : string.Empty;
                    }
                    return partDescription.Length > 0 ? $"{partDescription}" : string.Empty;
                })
                .Distinct()
                .Single(x => !string.IsNullOrWhiteSpace(x));
        }
        catch (InvalidOperationException)
        {
            // We show index + 1 to the user, as the usual CSV table viewers start with index 1, while row.Index is 0-based
            var rowIndexOneBased = row.Index + 1;

            var itemCode = ExtractItemCodeFromRowAssumingItsValid(row);

            throw new UserFriendlyLogException(
                $"Only one description attribute per item is allowed. CSV on row {rowIndexOneBased} (Item Code = {itemCode}) has one of the following problems: -- description is missing -- more than one distinct descriptions are filled in.",
                new ScaffoldingAttributeParsingException(
                    $"Description attributes cannot have multiple values. Only one weight attribute per item is allowed."
                )
            );
        }
    }

    /// <summary>
    /// Determines if the column at the given index in the row matches any of the provided numeric headers (case-insensitive).
    /// </summary>
    public static bool IsNumericSapColumn(ICsvLine row, int columnIndex, List<string> numericHeaders)
    {
        return numericHeaders.Any(h => string.Equals(h, row.Headers[columnIndex], StringComparison.OrdinalIgnoreCase));
    }
}
