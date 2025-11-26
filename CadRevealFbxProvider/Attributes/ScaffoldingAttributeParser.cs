namespace CadRevealFbxProvider.Attributes;

using System;
using System.Globalization;
using Csv;
using UserFriendlyLogger;

public class ScaffoldingAttributeParser
{
    private const string HeaderTotalWeight = "Grand total";
    private const string HeadingTotalWeightCalculated = "Grand total calculated";

    public static readonly List<string> AggregateAttributesPerModel_NumericHeadersSap =
    [
        "Work order",
        "Scaff build Operation Number",
        "Dismantle Operation number",
    ];

    public static readonly List<string> OtherAggregateAttributesPerModel =
    [
        "Scaff tag number",
        "Job pack",
        "Project number",
        "Planned build date",
        "Completion date",
        "Dismantle date",
        "Area",
        "Discipline",
        "Purpose",
        "Scaff type",
        "Load class",
        "Size (m\u00b3)",
        "Length (m)",
        "Width (m)",
        "Height (m)",
        "Covering (Y or N)",
        "Covering material",
        "Last Updated",
    ];

    public static readonly List<string> DescriptionAttributesPerItem = ["Description", "HAKI Description"];
    public static readonly List<string> WeightAttributesPerItem = ["Weight kg", "HAKI Weight", "Layher Weight", "Vekt"];
    public static readonly List<string> OtherAttributesPerItem = ["Count", "Manufacturer", "IfcGUID"];

    public static (
        Dictionary<string, Dictionary<string, string>?> attributesDictionary,
        ScaffoldingMetadata scaffoldingMetadata
    ) ParseAttributes(string[] fileLines, bool tempFlag = false)
    {
        ThrowExceptionIfEmptyCsv(fileLines);
        (fileLines, var tableContentOffset) = RemoveCsvNonDescriptionHeaderInfo(fileLines);
        ICsvLine[] attributeRawData = ConvertToCsvLines(fileLines, tableContentOffset);
        ValidateDataAndKeyAttribute(attributeRawData);
        ICsvLine lastAttributeLine = RetrieveLastCsvRowContainingWeight(attributeRawData);
        attributeRawData = RemoveLastCsvRowContainingWeigth(attributeRawData);
        var validatedAttributeData = RemoveCsvRowsWithoutKeyAttribute(
                attributeRawData,
                ScaffoldingCsvLineParser.ItemCodeColumnKey
            )
            .ToArray();

        var entireScaffoldingMetadata = new ScaffoldingMetadata();

        var allKeysAreValidAndUnique =
            validatedAttributeData.Select(x => x[ScaffoldingCsvLineParser.ItemCodeColumnKey]).Distinct().Count()
            == validatedAttributeData.Length;
        if (!allKeysAreValidAndUnique)
        {
            var duplicatedKeys = validatedAttributeData
                .Select(x => x[ScaffoldingCsvLineParser.ItemCodeColumnKey])
                .GroupBy(v => v)
                .Where(g => g.Count() > 1)
                .ToList();

            // format the list of duplicated keys for the error message as
            // -- key1 (count1), -- key2 (count2), ...
            var duplicatedKeysMsg = string.Join(", ", duplicatedKeys.Select(g => $"-- {g.Key} ({g.Count()}-times)"));

            throw new UserFriendlyLogException(
                $"CSV table column: \"{ScaffoldingCsvLineParser.ItemCodeColumnKey}\" should be unique for each row, but contains multiple rows with the same value. This indicates an export error, or maybe some items very inserted using copy-paste and the ID was not regenerated? The following values are duplicated: {duplicatedKeysMsg}. ",
                new ScaffoldingAttributeParsingException(
                    $"CSV table column: \"{ScaffoldingCsvLineParser.ItemCodeColumnKey}\" should be unique for each row"
                )
            );
        }

        var allColumnsHaveManufacturerHeader = validatedAttributeData.All(attrLine =>
            attrLine.Headers.Any(h => h.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase))
        );

        var attributesDictionary = validatedAttributeData.ToDictionary(
            x => ScaffoldingCsvLineParser.ExtractKeyFromCsvRow(x, ScaffoldingCsvLineParser.ItemCodeColumnKey),
            v =>
            {
                var kvp = new Dictionary<string, string>();

                var manufacturerColumnPresent = v.Headers.Any(h =>
                    h.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase)
                );
                var ifcGuidColumnPresent = v.Headers.Any(h => h.Equals("IfcGUID", StringComparison.OrdinalIgnoreCase));

                // In some cases, description and weight can appear in several columns (different manufacturers of item parts)
                // these columns need to be merged into one with manufacturer as prefix. Only a single manufacturer is allowed per
                // part. An exception will be thrown if more or less than one manufacturer is found per part.
                kvp["Description"] = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(
                    v,
                    manufacturerColumnPresent
                );

                // Weights are expected to either be in one column or the other, never both at the same time.
                // Merging is done by selecting the only non-empty string and throwing an exception if more than one non-empty.
                kvp["Weight kg"] = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(v) ?? "";

                // legacy, models using the old template will just have the fields empty
                kvp["Manufacturer"] =
                    (manufacturerColumnPresent)
                        ? ScaffoldingCsvLineParser.ExtractColumnValueFromRow("Manufacturer", v)
                        : "";
                kvp["IfcGUID"] =
                    (ifcGuidColumnPresent) ? ScaffoldingCsvLineParser.ExtractColumnValueFromRow("IfcGUID", v) : "";

                // not extracting this one yet, because not sure if useful
                kvp["Last Updated"] = "";
                kvp["Count"] = "";

                // Map all columns that are not aggregate columns from different manufacturers, such as description and weight
                for (int col = 0; col < v.ColumnCount; col++)
                {
                    // Ignore the key attribute
                    if (v.Headers[col] == ScaffoldingCsvLineParser.ItemCodeColumnKey)
                        continue;

                    // if it is not an aggregate attribute, skip it here
                    if (IsPurelyItemAttribute(v.Headers[col]))
                        continue;

                    var key = v.Headers[col].Trim();
                    var value = v[col].Trim();

                    // Check if numeric headers are actually numbers for non-temp scaffolds.
                    // For temporary scaffolds, we don't care.
                    if (
                        ScaffoldingCsvLineParser.IsNumericSapColumn(
                            v,
                            col,
                            AggregateAttributesPerModel_NumericHeadersSap
                        )
                    )
                    {
                        if (!tempFlag)
                        {
                            ThrowExceptionIfNotValidNumber(value, v.Headers[col]);
                            entireScaffoldingMetadata.TryAddValue(key, value);
                            kvp[key] = value;
                        }
                        else
                        {
                            // Temporary scaffold processing:
                            // We want to keep these fields undefined.
                            // Scaffold architect might have written something in these fields, thus we are possibly overriding that.
                            // We do not insert kvp in this case!
                            // Leaving the commented-out code as a reminder not to put this back.
                            // entireScaffoldingMetadata.TryAddValue(key, null);
                            // kvp[key] = null;
                        }
                    }
                    else
                    {
                        // non-numeric temp or non-temp scaffolding headers
                        WarnIfUnknownAggregateAttribute(v.Headers[col]);
                        entireScaffoldingMetadata.TryAddValue(key, value);
                        kvp[key] = value;
                    }
                }

                if (!ScaffoldingMetadata.PartMetadataHasExpectedValues(kvp, tempFlag))
                {
                    Console.WriteLine(
                        "Invalid attribute line: " + v[ScaffoldingCsvLineParser.ItemCodeColumnKey].ToString()
                    );
                    return null;
                }

                return kvp;
            }
        );

        bool hasItemsFromMultipleManufacturers =
            (allColumnsHaveManufacturerHeader)
                ? HasItemsFromMultipleManufacturers(attributesDictionary)
                : DetermineIfMultipleManufacturersFromDescription(attributesDictionary);

        if (hasItemsFromMultipleManufacturers)
        {
            Console.WriteLine("Warning: Items from multiple manufacturers detected in the attribute file.");
            Console.WriteLine(
                $"##teamcity[setParameter name='Scaffolding_WarningMessage' value='{"Using items from multiple manufacturers!"}']"
            );
        }

        float totalWeightCalculated = CalculateTotalWeightFromCsvPartEntries(attributesDictionary);
        entireScaffoldingMetadata.TryAddValue(
            HeadingTotalWeightCalculated,
            totalWeightCalculated.ToString(CultureInfo.InvariantCulture)
        );

        float totalWeight = RetrieveTotalWeightFromCsvAndThrowExceptionIfFail(lastAttributeLine);
        entireScaffoldingMetadata.TryAddValue(HeaderTotalWeight, totalWeight.ToString(CultureInfo.InvariantCulture));

        WarnIfSumOfPartWeightsDifferFromGiven(totalWeight, totalWeightCalculated);

        entireScaffoldingMetadata.TotalVolume = ConvertStringToEmptyIfNullOrWhiteSpace(
            entireScaffoldingMetadata.TotalVolume
        );

        // The building job should fail if mandatory metadata field(s) is/are missing
        // Otherwise the failure will happen later and the error message will be very cryptic
        entireScaffoldingMetadata.TempScaffoldingFlag = tempFlag;
        entireScaffoldingMetadata.ThrowIfModelMetadataInvalid(tempFlag);

        Console.WriteLine("Finished reading and processing attribute file.");
        return (attributesDictionary, entireScaffoldingMetadata);
    }

    private static bool IsPurelyItemAttribute(string attribute)
    {
        return DescriptionAttributesPerItem.Any(h => string.Equals(h, attribute, StringComparison.OrdinalIgnoreCase))
            || WeightAttributesPerItem.Any(h => string.Equals(h, attribute, StringComparison.OrdinalIgnoreCase))
            || OtherAttributesPerItem.Any(h => string.Equals(h, attribute, StringComparison.OrdinalIgnoreCase));
    }

    static void WarnIfSumOfPartWeightsDifferFromGiven(float givenTotalWeight, float summedTotalWeight)
    {
        if (Math.Abs(givenTotalWeight - summedTotalWeight) > 1.0E-3f)
            Console.WriteLine(
                $"Check total weight. Explicitly defined: {givenTotalWeight} and calculated: {summedTotalWeight}. Difference: {givenTotalWeight - summedTotalWeight}"
            );
    }

    private static string ConvertStringToEmptyIfNullOrWhiteSpace(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s;
    }

    private static void ThrowExceptionIfEmptyCsv(string[] fileLines)
    {
        if (fileLines.Length == 0)
            throw new UserFriendlyLogException(
                "The CSV file is either empty or does not contain any valid lines. Please check the CSV-template guide.",
                new ArgumentException("CSV file has no lines.", nameof(fileLines))
            );
        Console.WriteLine("Reading and processing attribute file.");
    }

    // returns the table with header, with everything else before header line removed, and the number of lines removed
    private static (string[], int) RemoveCsvNonDescriptionHeaderInfo(string[] fileLines)
    {
        // The below will remove the first row in the CSV file, if it is not the header.
        // We tried using CsvReader SkipRow, as well as similar options, but they did not work for header rows.
        return fileLines.First().Contains("Description") ? (fileLines, 0) : (fileLines.Skip(1).ToArray(), 1);
    }

    private static ICsvLine[] ConvertToCsvLines(string[] fileLines, int lineOffset)
    {
        return CsvReader
            .ReadFromText(
                String.Join(Environment.NewLine, fileLines),
                new CsvOptions()
                {
                    HeaderMode = HeaderMode.HeaderPresent,
                    RowsToSkip = 0,
                    Comparer = StringComparer.OrdinalIgnoreCase,
                    SkipRow = (row, idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
                    TrimData = true,
                    Separator = ';',
                }
            )
            .ToArray();
    }

    private static int ValidateDataAndKeyAttribute(ICsvLine[] attributeRawData)
    {
        if (attributeRawData.Length == 0)
            throw new UserFriendlyLogException(
                "The CSV table contains no data rows. Please check the exported attribute file.",
                new Exception("attributeRawData data is empty")
            );

        var columnIndexKeyAttribute = Array.IndexOf(
            attributeRawData.First().Headers,
            ScaffoldingCsvLineParser.ItemCodeColumnKey
        );

        if (columnIndexKeyAttribute < 0)
            throw new UserFriendlyLogException(
                "Missing column \"" + ScaffoldingCsvLineParser.ItemCodeColumnKey + "\" in the csv file",
                new Exception(
                    $"Key header {ScaffoldingCsvLineParser.ItemCodeColumnKey} is missing in the attribute file."
                )
            );

        return columnIndexKeyAttribute;
    }

    private static ICsvLine RetrieveLastCsvRowContainingWeight(ICsvLine[] attributeRawData)
    {
        // Total weight (model metadata, not per-part attribute) is stored in the last line of the attribute table
        return attributeRawData.Last();
    }

    private static ICsvLine[] RemoveLastCsvRowContainingWeigth(ICsvLine[] attributeRawData)
    {
        // Last line is skipped here, since it is not a per-part attribute (total weight)
        return attributeRawData.SkipLast(1).ToArray();
    }

    private static IEnumerable<ICsvLine> RemoveCsvRowsWithoutKeyAttribute(
        ICsvLine[] attributeRawData,
        string columnNameKeyAttribute
    )
    {
        // Validate raw data wrt missing "Item Code"
        var validatedAttributeData = attributeRawData.Where(item =>
            !string.IsNullOrWhiteSpace(item[columnNameKeyAttribute])
        );
        IEnumerable<ICsvLine> removeCsvRowsWithoutKeyAttribute =
            validatedAttributeData as ICsvLine[] ?? validatedAttributeData.ToArray();
        if (!removeCsvRowsWithoutKeyAttribute.Any())
        {
            throw new UserFriendlyLogException(
                $"Column: \"{ScaffoldingCsvLineParser.ItemCodeColumnKey}\" has no rows with an item code. Please check that the export is correct.",
                new ScaffoldingAttributeParsingException(
                    $"Column: \"{ScaffoldingCsvLineParser.ItemCodeColumnKey}\" has no rows with an item code"
                )
            );
        }

        Console.WriteLine(
            $"After attributes key validation, {removeCsvRowsWithoutKeyAttribute.Count()}/{attributeRawData.Length} items remain."
        );

        return removeCsvRowsWithoutKeyAttribute;
    }

    // returns true if there are items from multiple manufacturers in the attributes dictionary
    // works only if "Manufacturer" column exists in the file, not for older templates
    private static bool HasItemsFromMultipleManufacturers(
        Dictionary<string, Dictionary<string, string>?> attributesDictionary
    )
    {
        var manufacturers = attributesDictionary
            .Where(a => a.Value != null)
            .Select(av => av.Value!["Manufacturer"])
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();
        return manufacturers.Count > 1;
    }

    // returns true if there are items multiple manufacturers in the models
    // this method should be used when the column Manufacturers is not present
    // it looks at the description columns and checks if descriptions indicate several manufactuers
    private static bool DetermineIfMultipleManufacturersFromDescription(
        Dictionary<string, Dictionary<string, string>?> attributesDictionary
    )
    {
        // prior to the template change, only Aluhak and HAKI were actually used in the models
        bool hasHaki = attributesDictionary
            .Where(a => a.Value != null)
            .Select(av => av.Value!["Description"])
            .Any(desc => desc.StartsWith("HAKI", StringComparison.OrdinalIgnoreCase));

        bool hasOtherThanHaki = attributesDictionary
            .Where(a => a.Value != null)
            .Select(av => av.Value!["Description"])
            .Any(desc => !desc.StartsWith("HAKI", StringComparison.OrdinalIgnoreCase));

        return hasHaki && hasOtherThanHaki;
    }

    private static float CalculateTotalWeightFromCsvPartEntries(
        Dictionary<string, Dictionary<string, string>?> attributesDictionary
    )
    {
        // Calculate total weight from the table.
        // It will be used as a sanity check against the total weight explicitly written in the table
        // attributesDictionary
        return attributesDictionary
            .Where(a => a.Value != null)
            .Select(av =>
            {
                // weight is extracted from one of the weight columns, see ExtractSingleWeightFromCsvRow for details
                // and stored in "Weight kg"
                var w = av.Value!["Weight kg"];
                if (string.IsNullOrEmpty(w))
                    return 0; // sometime weight can be missing
                return float.Parse(w.Replace(" kg", String.Empty), CultureInfo.InvariantCulture);
            })
            .Sum();
    }

    private static float RetrieveTotalWeightFromCsvAndThrowExceptionIfFail(ICsvLine lastAttributeLine)
    {
        if (!lastAttributeLine[0].Contains(HeaderTotalWeight))
        {
            Console.Error.WriteLine("Attribute file does not contain total weight");
            throw new UserFriendlyLogException(
                "Total weight could not be extracted from the CSV file because it is missing. Please check the CSV-template guide.",
                new ScaffoldingAttributeParsingException("Attribute file does not contain total weight")
            );
        }

        // check if there is at least one weight entry in the total weight line
        var hasValidTotalWeight = lastAttributeLine.Values.Any(v => v.Contains("kg"));
        if (!hasValidTotalWeight)
        {
            const string errorMsg =
                "Grand total line in the attribute file does not contain any total weights. Maybe the correct export template was not used.";
            Console.Error.WriteLine("Error reading attribute file: " + errorMsg);
            throw new UserFriendlyLogException(errorMsg, new ScaffoldingAttributeParsingException(errorMsg));
        }

        // finds all partial total weights in the line (partial: per item producer)
        // and sums them up to the overall total weight
        var weights = lastAttributeLine
            .Values.Where(v => v.Contains("kg"))
            .Select(v =>
            {
                // strip the kg at the end of the number if it is there
                var w = v.Replace(" kg", String.Empty);
                try
                {
                    return float.Parse(w, CultureInfo.InvariantCulture);
                }
                catch (Exception exc)
                {
                    const string errorMsg = "Total weight line in the attribute file has an unknown format.";
                    Console.Error.WriteLine("Error reading attribute file: " + errorMsg);
                    throw new UserFriendlyLogException(
                        "Total weight could not be extracted from the CSV file. Please check the CSV-template guide.",
                        exc
                    );
                }
            });
        return weights.Sum(v => v);
    }

    private static void WarnIfUnknownAggregateAttribute(string attribute)
    {
        if (OtherAggregateAttributesPerModel.Any(h => string.Equals(h, attribute, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var errMsg = $"Warning parsing attribute file: Unknown column header {attribute}.";
        Console.Error.WriteLine(errMsg);
    }

    private static void ThrowExceptionIfNotValidNumber(string value, string columnName)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Parse will throw an assertion if it fails. Hence, we do not need the return value.
                float.Parse(value, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception)
        {
            var errMsg = $"Error parsing attribute value. {value} is not a valid {columnName}.";
            Console.Error.WriteLine(errMsg);
            throw new UserFriendlyLogException(
                $"{columnName} is expected to contain only numeric values, but exported CSV contains a non-numeric value {value}. Check the exported CSV.",
                new ScaffoldingAttributeParsingException(errMsg)
            );
        }
    }
}
