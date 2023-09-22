namespace CadRevealFbxProvider.Attributes;

using Csv;
using System.Text.Json;

public class ScaffoldingAttributeParser
{
    private static readonly string AttributeKey = "Item code";
    private static readonly int TotalWeightIndex = 1;
    private static readonly string HeaderTotalWeight = "Grand total";
    private static readonly int AttributeTableColCount = 23;

    public static readonly int NumberOfAttributesPerPart = 20;
    public static readonly int NumberOfModelAttributes = 4;

    public (Dictionary<string, Dictionary<string, string>> attributesDictionary, ScaffoldingMetadata scaffoldingMetadata) ParseAttributes(string[] fileLines)
    {
        Console.WriteLine("Reading attribute file.");

        var attributeRawData = CsvReader.ReadFromText(
            String.Join(Environment.NewLine, fileLines),
            new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                RowsToSkip = 0,
                SkipRow = (ReadOnlyMemory<char> row, int idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
                TrimData = true,
                Separator = ';'
            }
        ).ToArray();

        var itemCodeIdColumn = Array.IndexOf(attributeRawData.First().Headers, AttributeKey);

        if (itemCodeIdColumn < 0)
            throw new Exception("Key header \"" + AttributeKey + "\" is missing in the attribute file.");

        if (attributeRawData.First().ColumnCount == AttributeTableColCount)
        {
            var colCount = attributeRawData.First().ColumnCount;
            throw new Exception($"Attribute file contains {colCount}, expected a {AttributeTableColCount} attributes.");
        }

        var entireScaffoldingMetadata = new ScaffoldingMetadata();

        var attributesDictionary = attributeRawData.ToDictionary(
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

                return kvp;
            }
        );


        // total weight is stored in the last line of the attribute table
        var lastAttributeLine = attributeRawData.Last();

        if (lastAttributeLine[0].Contains(HeaderTotalWeight))
        {
            entireScaffoldingMetadata.TryAddValue(HeaderTotalWeight, lastAttributeLine[TotalWeightIndex].Trim());
        }
        else
        {
            throw new Exception("Attribute file does not contain total weight");
        }

        if (entireScaffoldingMetadata.HasExpectedValues())
        {
            Console.Error.WriteLine("Missing expected metadata: " + JsonSerializer.Serialize(entireScaffoldingMetadata));
        }

        return (attributesDictionary, entireScaffoldingMetadata);
    }
}
