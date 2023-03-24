namespace CadRevealFbxProvider.Attributes;

using Csv;

public class ScaffoldingAttributeParser
{
    private static readonly string AttributeKey = "Item code";
    public Dictionary<string, Dictionary<string, string>> ParseAttributes(string[] fileLines)
    {
        var attributeRawData = CsvReader.ReadFromText(String.Join(Environment.NewLine, fileLines), new CsvOptions()
        {
            HeaderMode = HeaderMode.HeaderPresent,
            RowsToSkip = 0,
            SkipRow = (ReadOnlyMemory<char> row, int idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
            TrimData = true,
            Separator = ';'
        });

        var indexIdColumn = Array.IndexOf(attributeRawData.First().Headers, AttributeKey);

        if (indexIdColumn < 0)
            throw new Exception("Key header \"" + AttributeKey + "\" is missing in the attribute file.");

        if (attributeRawData.First().ColumnCount == 23)
            throw new Exception($"Attribute file contains {attributeRawData.First().ColumnCount} a table with 23 attributes.");

        var attributesDictionary = attributeRawData.ToDictionary(x => x.Values[indexIdColumn], v =>
        {
            var kvp = new Dictionary<string, string>();

            for (int col = 0; col < v.ColumnCount; col++)
            {
                if (indexIdColumn == col) continue;

                var header = v.Headers[col];
                var value = v.Values[col];
                kvp[header] =  value;
            }

            return kvp;
        });

        return attributesDictionary;
    }
}