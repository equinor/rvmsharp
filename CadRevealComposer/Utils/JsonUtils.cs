namespace CadRevealComposer.Utils;

using System.IO;
using System.Text.Json;

public static class JsonUtils
{
    public static void JsonSerializeToFile<T>(T obj, string filename, bool writeIndented = false)
    {
        var jsonData = JsonSerializer.Serialize(
            obj,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = writeIndented }
        );
        File.WriteAllText(filename, jsonData);
    }
}
