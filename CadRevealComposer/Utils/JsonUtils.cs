namespace CadRevealComposer.Utils;

using System.IO;
using System.Text.Json;

public static class JsonUtils
{
    public static void JsonSerializeToFile<T>(T obj, string filename, bool formatIndented = false)
    {
        using var stream = File.Create(filename);
        using var writer = new StreamWriter(stream);
        JsonSerializer.Serialize(stream, obj,
            new JsonSerializerOptions(JsonSerializerDefaults.Web /* Makes properties lower-case by default */) { WriteIndented = formatIndented });
    }
}