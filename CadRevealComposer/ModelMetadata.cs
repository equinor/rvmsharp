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

    public static string MakeSerializable(ModelMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata.metadata, new JsonSerializerOptions { WriteIndented = true });
    }
}
