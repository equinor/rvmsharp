namespace CadRevealFbxProvider.Attributes;

using CadRevealFbxProvider.UserFriendlyLogger;
using Csv;

public static class ScaffoldingCsvLineParser
{
    public const string RowIndexColumnKey = "Row index";

    public const string ItemCodeColumnKey = "Item code";

    /// <summary>
    /// Extracts the value for a given row number from a CSV row
    /// The row number is calculated and inserted into the row during preprocessing, thus it should not be missing.
    /// </summary>
    public static string ExtractRowNumberFromRow(ICsvLine row)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el => el.header.Equals(RowIndexColumnKey, StringComparison.OrdinalIgnoreCase))
            .Select(el => row.Values[el.index])
            .Single(x => !String.IsNullOrWhiteSpace(x));
    }

    /// <summary>
    /// Extracts the value for a given item code from a CSV row
    /// The presence of item code is mandatory and there should be guards for its presence before executing this method.
    /// However, to make this fail-proof, this method will catch the exception and return an "UNDEFINED" if the item code is missing or ill-defined.
    /// </summary>
    public static string ExtractItemCodeFromRow(ICsvLine row)
    {
        try
        {
            return row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Equals(ItemCodeColumnKey, StringComparison.OrdinalIgnoreCase))
                .Select(el => row.Values[el.index])
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
    public static string ExtractKeyFromCsvRow(ICsvLine row, int columnIndexKey, string keyAttribute)
    {
        var key = row.Values[columnIndexKey];
        if (string.IsNullOrEmpty(key))
            throw new ScaffoldingAttributeParsingException($"Key attribute {keyAttribute} cannot have missing values.");
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
                .Select(el => row.Values[el.index])
                .Single(x => !String.IsNullOrWhiteSpace(x));
        }
        catch (InvalidOperationException)
        {
            var rowIndex = ExtractRowNumberFromRow(row);
            var itemCode = ExtractItemCodeFromRow(row);

            throw new UserFriendlyLogException(
                $"CSV file has one or more of the following issues with the column \"{columnHeader}\": -- it is missing entirely  -- occurs multiple times with distinct values -- it has a missing value on row {rowIndex} (Item code = {itemCode}",
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
                .Select(el => row.Values[el.index])
                .Distinct()
                .SingleOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }
        catch (InvalidOperationException)
        {
            // this should not throw, as this is a column that is created programatically during processing
            var rowIndex = ExtractRowNumberFromRow(row);

            var itemCode = row
                .Headers.Select((h, i) => new { header = h, index = i })
                .Where(el => el.header.Equals(ItemCodeColumnKey, StringComparison.OrdinalIgnoreCase))
                .Select(el => row.Values[el.index])
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
                    var partDescription = row.Values[el.index];
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
            var rowIndex = ExtractRowNumberFromRow(row);
            var itemCode = ExtractItemCodeFromRow(row);

            throw new UserFriendlyLogException(
                $"Only one description attribute per item is allowed. CSV on row {rowIndex} (Item Code = {itemCode}) has one of the following problems: -- description is missing -- more than one distinct descriptions are filled in.",
                new ScaffoldingAttributeParsingException(
                    $"Description attributes cannot have multiple values. Only one weight attribute per item is allowed."
                )
            );
        }
    }

    /// <summary>
    /// prepends the rown number to each row starting from 1, not from 0
    /// if the Table needs to be debugged, this helps to identify the row in the original CSV file
    /// </summary>
    public static string[] PrependRowNumberToCsvLines(string[] fileLines, int tableOffset)
    {
        return fileLines
            .Select(
                (line, index) =>
                {
                    if (index == 0)
                        return $"\"{RowIndexColumnKey}\";{line}";
                    ; // do not prepend row number to header line, but a header for the row number column
                    if (line.Length == 0)
                        return line; // do not prepend row number to empty lines
                    if (line.StartsWith("Grand total"))
                        return line; // do not prepend row number to the last line with total weight
                    return $"\"{index + tableOffset + 1}\";{line}";
                    // +1, because row number start with 1, not 0
                }
            )
            .ToArray();
    }

    /// <summary>
    /// Determines if the column at the given index in the row matches any of the provided numeric headers (case-insensitive).
    /// </summary>
    public static bool IsNumericSapColumn(ICsvLine row, int columnIndex, List<string> numericHeaders)
    {
        return numericHeaders.Any(h => string.Equals(h, row.Headers[columnIndex], StringComparison.OrdinalIgnoreCase));
    }
}
