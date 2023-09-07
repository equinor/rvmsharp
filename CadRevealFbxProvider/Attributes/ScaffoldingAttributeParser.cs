namespace CadRevealFbxProvider.Attributes;

using CadRevealComposer;
using Csv;

public class ScaffoldingAttributeParser
{
    private static readonly string AttributeKey = "Item code";
    private static readonly int TotalWeightIndex = 1;
    private static readonly string HeaderTotalWeight = "Grand total";
    private static readonly int AttributeTableColCount = 23;

    public static readonly int NumberOfAttributesPerPart = 20;
    public static readonly int NumberOfModelAttributes = 4;

    public (Dictionary<string, Dictionary<string, string>?>, ModelMetadata) ParseAttributes(string[] fileLines)
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
        );

        var indexIdColumn = Array.IndexOf(attributeRawData.First().Headers, AttributeKey);

        if (indexIdColumn < 0)
            throw new Exception("Key header \"" + AttributeKey + "\" is missing in the attribute file.");

        if (attributeRawData.First().ColumnCount == AttributeTableColCount)
        {
            var colCount = attributeRawData.First().ColumnCount;
            throw new Exception($"Attribute file contains {colCount}, expected a {AttributeTableColCount} attributes.");
        }

        var metadata = new Dictionary<string, string>();

        var attributesDictionary = attributeRawData.ToDictionary(
            x => x.Values[indexIdColumn],
            v =>
            {
                var kvp = new Dictionary<string, string>();

                for (int col = 0; col < v.ColumnCount; col++)
                {
                    if (indexIdColumn == col)
                        continue;

                    if (ScaffoldingMetadata.ModelAttributes.Any(s => v.Headers[col].Contains(s)))
                    {
                        var metadataEntryKey = v.Headers[col];
                        var mappedKey = ScaffoldingMetadata.AttributeMap[metadataEntryKey];
                        var attrValue = v.Values[col];
                        if (attrValue.Length > 0)
                        {
                            var valueExists = metadata.ContainsKey(mappedKey);
                            if (valueExists)
                            {
                                var existingValue = metadata[mappedKey];
                                if (attrValue != existingValue)
                                {
                                    throw new Exception(
                                        "Attribute file contains multiple work orders ("
                                            + attrValue
                                            + ","
                                            + existingValue
                                            + ")"
                                    );
                                }
                            }
                            else
                            {
                                metadata.Add(mappedKey, attrValue);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid attribute line: " + v[indexIdColumn].ToString());
                            return null;
                        }
                    }
                    else
                    {
                        var header = v.Headers[col];
                        var value = v.Values[col];
                        kvp[header] = value;
                    }
                }
                if (metadata.Count < ScaffoldingMetadata.ModelAttributes.Length)
                {
                    Console.WriteLine("Invalid metadata: " + v[indexIdColumn].ToString());
                    return null;
                }

                return kvp;
            }
        );

        // total weight is stored in the last line of the attribute table
        var lastAttributeLine = attributeRawData.Last();

        if (lastAttributeLine[0].Contains(HeaderTotalWeight))
        {
            var mappedKey = ScaffoldingMetadata.AttributeMap[HeaderTotalWeight];
            metadata.Add(mappedKey, lastAttributeLine[TotalWeightIndex]);
        }
        else
        {
            throw new Exception("Attribute file does not contain total weight");
        }

        return (attributesDictionary, new ModelMetadata(metadata));
    }
}
