namespace CadRevealFbxProvider.Attributes;

using System.Globalization;
using System.Text.Json;
using Csv;

public class ScaffoldingAttributeParser
{
    public const int NumberOfAttributesPerPart = 23; // all attributes including 3 out of 4 model attributes

    private const string HeaderTotalWeight = "Grand total";
    private const string HeadingTotalWeightCalculated = "Grand total calculated";
    private const string KeyAttribute = "Item code";

    public static readonly List<string> NumericHeadersSap =
    [
        "Work order",
        "Scaff build Operation Number",
        "Dismantle Operation number",
    ];

    public static readonly List<string> OtherManufacturerIndependentAttributesPerPart =
    [
        "Count",
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
        "Length(m)",
        "Width(m)",
        "Height(m)",
        "Covering (Y or N)",
        "Covering material",
        "Last Updated",
    ];

    public static (
        Dictionary<string, Dictionary<string, string>?> attributesDictionary,
        ScaffoldingMetadata scaffoldingMetadata
    ) ParseAttributes(string[] fileLines, bool tempFlag = false)
    {
        ThrowExceptionIfEmptyCsv(fileLines);
        fileLines = RemoveCsvNonDescriptionHeaderInfo(fileLines);
        ICsvLine[] attributeRawData = ConvertToCsvLines(fileLines);
        int columnIndexKeyAttribute = RetrieveKeyAttributeColumnIndex(attributeRawData);
        ICsvLine lastAttributeLine = RetrieveLastCsvRowContainingWeight(attributeRawData);
        attributeRawData = RemoveLastCsvRowContainingWeigth(attributeRawData);
        var validatedAttributeData = RemoveCsvRowsWithoutKeyAttribute(attributeRawData, columnIndexKeyAttribute);

        var entireScaffoldingMetadata = new ScaffoldingMetadata();

        var attributesDictionary = validatedAttributeData.ToDictionary(
            x => ExtractKeyFromCsvRow(x, columnIndexKeyAttribute),
            v =>
            {
                var kvp = new Dictionary<string, string>();

                // In some cases, description and weight can appear in several columns (different manufacturers of item parts)
                // these columns need to be merged into one with manufacturer as prefix. Only a single manufacturer is allowed per
                // part. An exception will be thrown if more or less than one manufacturer is found per part.
                kvp["Description"] = GenPartDescriptionWithSingleManufacturerPrefixFromHeadings(v);

                // Weights are expected to either be in one column or the other, never both at the same time.
                // Merging is done by selecting the only non-empty string and throwing an exception if more than one non-empty.
                kvp["Weight kg"] = ExtractSingleWeightRelatedValueFromCsvRow(v) ?? "";

                // Map all columns that are not aggregate columns from different manufacturers, such as description and weight
                for (int col = 0; col < v.ColumnCount; col++)
                {
                    // Ignore the key attribute
                    if (columnIndexKeyAttribute == col)
                        continue;

                    // Ignore description and weight, they are added as an aggregate of several columns
                    if (v.Headers[col].Contains("description", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (v.Headers[col].Contains("weight", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var key = v.Headers[col].Trim();
                    var value = v.Values[col].Trim();

                    // Check if numeric headers are actually numbers for non-temp scaffolds.
                    // For temporary scaffolds, we don't care.
                    if (IsNumericSapColumn(v, col))
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
                        WarnIfUnknownManufacturerIndependentAttribute(v.Headers[col]);
                        entireScaffoldingMetadata.TryAddValue(key, value);
                        kvp[key] = value;
                    }
                }

                if (!ScaffoldingMetadata.PartMetadataHasExpectedValues(kvp, tempFlag))
                {
                    Console.WriteLine("Invalid attribute line: " + v[columnIndexKeyAttribute].ToString());
                    return null;
                }

                return kvp;
            }
        );

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
        ThrowIfModelMetadataDoesNotHaveExpectedValues(entireScaffoldingMetadata, tempFlag);
        entireScaffoldingMetadata.TempScaffoldingFlag = tempFlag;

        Console.WriteLine("Finished reading and processing attribute file.");
        return (attributesDictionary, entireScaffoldingMetadata);
    }

    static void ThrowIfModelMetadataDoesNotHaveExpectedValues(
        ScaffoldingMetadata entireScaffoldingMetadata,
        bool tempFlag
    )
    {
        if (!entireScaffoldingMetadata.ModelMetadataHasExpectedValues(tempFlag))
        {
            Console.WriteLine("Missing expected metadata: " + JsonSerializer.Serialize(entireScaffoldingMetadata));
        }
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
            throw new ArgumentException(null, nameof(fileLines));
        Console.WriteLine("Reading and processing attribute file.");
    }

    private static string[] RemoveCsvNonDescriptionHeaderInfo(string[] fileLines)
    {
        // The below will remove the first row in the CSV file, if it is not the header.
        // We tried using CsvReader SkipRow, as well as similar options, but they did not work for header rows.
        return fileLines.First().Contains("Description") ? fileLines : fileLines.Skip(1).ToArray();
    }

    private static ICsvLine[] ConvertToCsvLines(string[] fileLines)
    {
        return CsvReader
            .ReadFromText(
                String.Join(Environment.NewLine, fileLines),
                new CsvOptions()
                {
                    HeaderMode = HeaderMode.HeaderPresent,
                    RowsToSkip = 0,
                    SkipRow = (ReadOnlyMemory<char> row, int idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
                    TrimData = true,
                    Separator = ';',
                }
            )
            .ToArray();
    }

    private static int RetrieveKeyAttributeColumnIndex(ICsvLine[] attributeRawData)
    {
        var columnIndexKeyAttribute = Array.IndexOf(attributeRawData.First().Headers, KeyAttribute);

        if (columnIndexKeyAttribute < 0)
            throw new Exception($"Key header {KeyAttribute} is missing in the attribute file.");

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
        int columnIndexKeyAttribute
    )
    {
        // Validate raw data wrt missing "Item Code"
        var validatedAttributeData = attributeRawData.Where(item =>
            !string.IsNullOrWhiteSpace(item.Values[columnIndexKeyAttribute])
        );
        IEnumerable<ICsvLine> removeCsvRowsWithoutKeyAttribute =
            validatedAttributeData as ICsvLine[] ?? validatedAttributeData.ToArray();
        if (!removeCsvRowsWithoutKeyAttribute.Any())
        {
            throw new Exception($"{KeyAttribute} cannot be missing for all items.");
        }

        Console.WriteLine(
            $"After attributes key validation, {removeCsvRowsWithoutKeyAttribute.Count()}/{attributeRawData.Length} items remain."
        );

        return removeCsvRowsWithoutKeyAttribute;
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
                var w = av.Value!["Weight kg"];
                if (string.IsNullOrEmpty(w))
                    return 0; // sometime weight can be missing
                return float.Parse(w.Replace(" kg", String.Empty), CultureInfo.InvariantCulture);
            })
            .Sum();
    }

    private static float RetrieveTotalWeightFromCsvAndThrowExceptionIfFail(ICsvLine lastAttributeLine)
    {
        float totalWeight = 0.0f;

        // finds all partial total weights in the line (partial: per item producer)
        // and sums them up to the overall total weight
        if (lastAttributeLine[0].Contains(HeaderTotalWeight))
        {
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
                    catch
                    {
                        const string errorMsg = "Total weight line in the attribute file has an unknown format.";
                        Console.Error.WriteLine("Error reading attribute file: " + errorMsg);
                        throw new Exception(errorMsg);
                    }
                });
            totalWeight = weights.Sum(v => v);
        }
        else
        {
            Console.Error.WriteLine("Attribute file does not contain total weight");
            throw new Exception("Attribute file does not contain total weight");
        }

        return totalWeight;
    }

    private static void WarnIfUnknownManufacturerIndependentAttribute(string attribute)
    {
        if (
            OtherManufacturerIndependentAttributesPerPart.Any(h =>
                string.Equals(h, attribute, StringComparison.OrdinalIgnoreCase)
            )
        )
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
            throw new Exception(errMsg);
        }
    }

    private static bool IsNumericSapColumn(ICsvLine row, int columnIndex)
    {
        return NumericHeadersSap.Any(h =>
            string.Equals(h, row.Headers[columnIndex], StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string? ExtractSingleWeightRelatedValueFromCsvRow(ICsvLine row)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el => el.header.Contains("weight", StringComparison.OrdinalIgnoreCase))
            .Select(el => row.Values[el.index])
            .SingleOrDefault(x => !String.IsNullOrWhiteSpace(x));
    }

    private static string GenPartDescriptionWithSingleManufacturerPrefixFromHeadings(ICsvLine row)
    {
        return row
            .Headers.Select((h, i) => new { header = h, index = i })
            .Where(el => el.header.Contains("description", StringComparison.OrdinalIgnoreCase))
            .Select(el =>
            {
                var manufacturerName = el.header.ToUpper().Replace("DESCRIPTION", String.Empty).Trim();
                var spacer = (manufacturerName.Length > 0) ? " " : "";
                var partDescription = row.Values[el.index];
                return partDescription.Length > 0 ? $"{manufacturerName}{spacer}{partDescription}" : String.Empty;
            })
            .Single(x => !String.IsNullOrWhiteSpace(x));
    }

    private static string ExtractKeyFromCsvRow(ICsvLine row, int columnIndexKey)
    {
        var key = row.Values[columnIndexKey];
        if (string.IsNullOrEmpty(key))
            throw new Exception($"{KeyAttribute} cannot have missing values.");
        return key;
    }
}
