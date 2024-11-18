namespace CadRevealComposer;

using System.Collections.Generic;
using System.Text.Json;

public class ModelMetadata(Dictionary<string, string> metadata)
{
    private readonly Dictionary<string, string> _metadata = metadata;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public int Count()
    {
        return this._metadata.Count;
    }

    public void Add(ModelMetadata modelMetadata)
    {
        foreach (var kvp in modelMetadata._metadata)
        {
            // This will throw if the key already exists!:)
            _metadata.Add(kvp.Key, kvp.Value);
        }
    }

    public static string Serialize(ModelMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata._metadata, JsonSerializerOptions);
    }
}
