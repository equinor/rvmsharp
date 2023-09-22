namespace CadRevealComposer;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

public class ModelMetadata
{
    Dictionary<string, string> metadata;

    public ModelMetadata(Dictionary<string, string> _metadata)
    {
        metadata = _metadata;
    }

    public int Count()
    {
        return this.metadata.Count;
    }

    public void Add(ModelMetadata modelMetadata)
    {
        foreach (var kvp in modelMetadata.metadata)
        {
            // This will throw if the key already exists!:)
            this.metadata.Add(kvp.Key, kvp.Value);
        }
    }

    public static string Serialize(ModelMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata.metadata, new JsonSerializerOptions { WriteIndented = true });
    }
}
