namespace CadRevealFbxProvider.Attributes;

using Csv;

public class ScaffoldingAttributeParser
{
    public Dictionary<string, Dictionary<string, string>> ParseAttributes(string[] fileLines)
    {
        // TODO: Add unit tests
        // TODO: Refactor ParseAttributes
        var attributeRawData = CsvReader.ReadFromText(String.Join(Environment.NewLine, fileLines), new CsvOptions()
        {
            HeaderMode = HeaderMode.HeaderPresent,
            RowsToSkip = 0,
            SkipRow = (ReadOnlyMemory<char> row, int idx) => row.Span.IsEmpty || row.Span[0] == '#' || idx == 2,
            TrimData = true,
            Separator = ';'
        });

//<<<<<<< HEAD
//        var idNummerCol = Array.IndexOf(
//            data.First().Headers, "Equinor ID nummer");
//        //var datas = data.ToDictionary(x => x.Values[0], v =>
//        var datas = data.ToDictionary(x => x.Values[idNummerCol], v =>
//        {
//            var kvp = new Dictionary<string, string>();
//            for (int col = 0; col < v.ColumnCount ; col++)
//=======
        var indexIdColumn = Array.IndexOf(attributeRawData.First().Headers, "Item code");
        //int keyHeaderIndex = attributeRawData.First().ColumnCount - 1;
        //var keyHeader = attributeRawData.First().Headers[keyHeaderIndex];
        if (indexIdColumn < 0)
            throw new Exception("Key header \"Item code\" is missing in the attribute file.");

        var attribtuesDictionary = attributeRawData.ToDictionary(x => x.Values[indexIdColumn], v =>
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

        return attribtuesDictionary;
    }
}