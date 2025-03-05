namespace CadRevealFbxProvider.Attributes;

using System.Text.Json;
using Csv;

public class ScaffoldingAttributeParser
{
    private static readonly string AttributeKey = "Item code";
    private static readonly int TotalWeightIndex = 1;
    private static readonly string HeaderTotalWeight = "Grand total";
    private static readonly int AttributeTableColCount = 23;

    public static readonly int NumberOfAttributesPerPart = 23; // all attributes including 3 out of 4 model attributes

    private static string ConvertStringToEmptyIfNullOrWhiteSpace(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s;
    }

    public (
        Dictionary<string, Dictionary<string, string>?> attributesDictionary,
        ScaffoldingMetadata scaffoldingMetadata
    ) ParseAttributes(string[] fileLines)
    {
        if (fileLines.Length == 0)
            throw new ArgumentException(nameof(fileLines));
        Console.WriteLine("Reading attribute file.");

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
                    Separator = ';',
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
                    for (int col = 0; col < v.ColumnCount; col++)
                    {
                        if (itemCodeIdColumn == col)
                            continue; // Ignore it
                        var key = v.Headers[col].Trim();
                        var value = v.Values[col].Trim();
                        entireScaffoldingMetadata.TryAddValue(key, value);
                        kvp[key] = value;
                    }
                    if (!ScaffoldingMetadata.HasExpectedValuesFromAttributesPerPart(kvp))
                    {
                        Console.WriteLine("Invalid attribute line: " + v[itemCodeIdColumn].ToString());
                        return null;
                    }
                    return kvp;
                }
            );

        if (lastAttributeLine[0].Contains(HeaderTotalWeight))
        {
            entireScaffoldingMetadata.TryAddValue(HeaderTotalWeight, lastAttributeLine[TotalWeightIndex].Trim());
        }
        else
        {
            throw new Exception("Attribute file does not contain total weight");
        }

        if (!entireScaffoldingMetadata.HasExpectedValues())
        {
            Console.Error.WriteLine(
                "Missing expected metadata: " + JsonSerializer.Serialize(entireScaffoldingMetadata)
            );
        }

        entireScaffoldingMetadata.TotalVolume = ConvertStringToEmptyIfNullOrWhiteSpace(
            entireScaffoldingMetadata.TotalVolume
        );

        return (attributesDictionary, entireScaffoldingMetadata);
    }
}
