namespace CadRevealFbxProvider.Attributes;

using Csv;

public class ScaffoldingAttributeParser
{
    public Dictionary<string, Dictionary<string, string>> ParseAttributes(string[] fileLines)
    {
        // TODO: Add unit tests
        // TODO: Refactor ParseAttributes

        var data = CsvReader.ReadFromText(
            String.Join(Environment.NewLine, fileLines),
            new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                RowsToSkip = 1,
                TrimData = true,
                Separator = ';'
        });

        var idNummerCol = Array.IndexOf(
            data.First().Headers, "Equinor ID nummer");
        //var datas = data.ToDictionary(x => x.Values[0], v =>
        var datas = data.ToDictionary(
            x => x.Values[idNummerCol],
            v =>
            {
                var kvp = new Dictionary<string, string>();
                for (int col = 0; col < v.ColumnCount; col++)
                {
                    var header = v.Headers[col];
                    var value = v.Values[col];
                    kvp[header] = value;
                }
                return kvp;
            }
        );

        return datas;
    }
}
