namespace CadRevealComposer.Utils;

using Newtonsoft.Json;
using System.IO;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

public static class JsonUtils
{
    public static void JsonSerializeToFile<T>(T obj, string filename, Formatting formatting = Formatting.None)
    {
        using var stream = File.Create(filename);
        using var writer = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(writer);
        JsonSerializer.Serialize(stream, obj, new JsonSerializerOptions(JsonSerializerDefaults.Web){ WriteIndented = formatting == Formatting.Indented});
    }
}