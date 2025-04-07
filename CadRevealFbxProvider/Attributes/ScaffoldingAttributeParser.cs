namespace CadRevealFbxProvider.Attributes;

using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Windows.Markup;
using Csv;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;

public class ScaffoldingAttributeParser
{
    private static readonly string AttributeKey = "Item code";
    private static readonly string HeaderTotalWeight = "Grand total";
    private static readonly int AttributeTableColCount = 23;

    public static readonly int NumberOfAttributesPerPart = 23; // all attributes including 3 out of 4 model attributes
    public static readonly List<string> NumericHeadersSAP = new List<string>
    {
        "Work order",
        "Scaff build Operation Number",
        "Dismantle Operation number"
    };

    private static string ConvertStringToEmptyIfNullOrWhiteSpace(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s;
    }

    public (
        Dictionary<string, Dictionary<string, string>?> attributesDictionary,
        ScaffoldingMetadata scaffoldingMetadata
    ) ParseAttributes(string[] fileLines, bool tempFlag = false)
    {
        if (fileLines.Length == 0)
            throw new ArgumentException(nameof(fileLines));
        Console.WriteLine("Reading and processing attribute file.");

        // The below will remove the first row in the CSV file, if it is not the header.
        // We tried using CsvReader SkipRow, as well as similar options, but they did not work for header rows.
        if (!fileLines.First().Contains("Description"))
        {
            fileLines = fileLines.Skip(1).ToArray();
        }

        var attributeRawData = CsvReader
            .ReadFromText(
                String.Join(Environment.NewLine, fileLines),
                new CsvOptions()
                {
                    HeaderMode = HeaderMode.HeaderPresent,
                    RowsToSkip = 0,
                    SkipRow = (ReadOnlyMemory<char> row, int idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
                    TrimData = true,
                    Separator = ';'
                }
            )
            .ToArray();

        var itemCodeIdColumn = Array.IndexOf(attributeRawData.First().Headers, AttributeKey);

        if (itemCodeIdColumn < 0)
            throw new Exception("Key header \"" + AttributeKey + "\" is missing in the attribute file.");

        if (attributeRawData.First().ColumnCount == AttributeTableColCount)
        {
            var colCount = attributeRawData.First().ColumnCount;
            throw new Exception($"Attribute file contains {colCount}, expected a {AttributeTableColCount} attributes.");
        }

        var entireScaffoldingMetadata = new ScaffoldingMetadata();

        // total weight (model metadata, not per-part attribute) is stored in the last line of the attribute table
        var lastAttributeLine = attributeRawData.Last();
        // last line is skipped here, since it is not a per-part attribute
        var attributesDictionary = attributeRawData
            .SkipLast(1)
            .ToDictionary(
                x => x.Values[itemCodeIdColumn],
                v =>
                {
                    var kvp = new Dictionary<string, string>();

                    // in some cases, description and weight can appear in several columns (different manufacturers of item parts)
                    // these columns need to be merged into one

                    var description = v
                        .Headers.Select((h, i) => new { header = h, index = i })
                        .Where(el => el.header.Contains("description", StringComparison.OrdinalIgnoreCase))
                        .Select(el =>
                        {
                            var manufacturerName = el
                                .header.ToLower()
                                .Replace("description", String.Empty)
                                .ToUpper()
                                .Trim();
                            var spacer = (manufacturerName.Length > 0) ? " " : "";
                            var partDescription = v.Values[el.index];
                            if (partDescription.Length > 0)
                                return manufacturerName + spacer + partDescription;

                            return String.Empty;
                        })
                        .ToList();

                    kvp["Description"] = String.Join(String.Empty, description);

                    var weights = v
                        .Headers.Select((h, i) => new { header = h, index = i })
                        .Where(el => el.header.Contains("weight", StringComparison.OrdinalIgnoreCase))
                        .Select(el => v.Values[el.index]);

                    // weights are expected to either in one column or the other, never both at the same time
                    // there merging them is done via joining the strings
                    kvp["Weight kg"] = String.Join(String.Empty, weights);

                    for (int col = 0; col < v.ColumnCount; col++)
                    {
                        if (itemCodeIdColumn == col)
                            continue; // Ignore it

                        // ignore description and weight, they are added as an aggregate of several columns
                        if (v.Headers[col].Contains("description", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (v.Headers[col].Contains("weight", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var key = v.Headers[col].Trim();
                        var value = v.Values[col].Trim();

                        // check if numeric headers are actually numbers for non-temp scaffs
                        // for temp scaffs, we don't care
                        if (
                            NumericHeadersSAP.Any(h =>
                                string.Equals(h, v.Headers[col], StringComparison.OrdinalIgnoreCase)
                            )
                        )
                        {
                            if (!tempFlag)
                            {
                                var NumFieldValueCandidate = value;
                                try
                                {
                                    float.Parse(NumFieldValueCandidate, CultureInfo.InvariantCulture);
                                }
                                catch (Exception)
                                {
                                    var errMsg =
                                        $"Error parsing attribute value. {NumFieldValueCandidate} is not a valid {v.Headers[col]}.";
                                    Console.Error.WriteLine(errMsg);
                                    throw new Exception(errMsg);
                                }

                                entireScaffoldingMetadata.TryAddValue(key, NumFieldValueCandidate);
                                kvp[key] = NumFieldValueCandidate;
                            }
                            else
                            {
                                // temp scaff processing:
                                // we want to keep these fields undefined
                                // scaff architect might have written something in these fields, thus we are possibly overriding that
                                // we do not insert kvp in this case!
                                // leaving the commented-out code as a reminder not to but this back
                                // entireScaffoldingMetadata.TryAddValue(key, null);
                                // kvp[key] = null;
                            }
                        }
                        else
                        {
                            // non-numeric temp or non-temp scaffolding headers
                            entireScaffoldingMetadata.TryAddValue(key, value);
                            kvp[key] = value;
                        }
                    }

                    if (!ScaffoldingMetadata.PartMetadataHasExpectedValues(kvp, tempFlag))
                    {
                        Console.WriteLine("Invalid attribute line: " + v[itemCodeIdColumn].ToString());
                        return null;
                    }

                    return kvp;
                }
            );

        var totalWeightCalculated = attributesDictionary
            .Where(a => a.Value != null)
            .Select(av =>
            {
                var w = av.Value!["Weight kg"];
                if (w!.Length == 0)
                    return 0; // sometime weight can be missing
                return float.Parse(w.Replace(" kg", String.Empty), CultureInfo.InvariantCulture);
            })
            .Sum();

        // calculate total weight from the table
        // will be used as a sanity check against the total weight explicitly written in the table
        // attributesDictionary

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
            var totalWeight = weights.Sum(v => v);
            entireScaffoldingMetadata.TryAddValue(
                HeaderTotalWeight,
                totalWeight.ToString(CultureInfo.InvariantCulture)
            );

            entireScaffoldingMetadata.TryAddValue(
                "Total weight calc",
                totalWeightCalculated.ToString(CultureInfo.InvariantCulture)
            );

            if (totalWeight != totalWeightCalculated)
                Console.WriteLine(
                    $"Check total weight. Explicitly defined: {totalWeight} and calculated: {totalWeightCalculated}. Difference: {totalWeight - totalWeightCalculated}"
                );
        }
        else
        {
            Console.Error.WriteLine("Attribute file does not contain total weight");
            throw new Exception("Attribute file does not contain total weight");
        }

        entireScaffoldingMetadata.TotalVolume = ConvertStringToEmptyIfNullOrWhiteSpace(
            entireScaffoldingMetadata.TotalVolume
        );

        if (!entireScaffoldingMetadata.ModelMetadataHasExpectedValues(tempFlag))
        {
            Console.Error.WriteLine(
                "Missing expected metadata: " + JsonSerializer.Serialize(entireScaffoldingMetadata)
            );
        }

        

        Console.WriteLine("Finished reading and processing attribute file.");
        return (attributesDictionary, entireScaffoldingMetadata);
    }
}
