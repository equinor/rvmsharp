namespace CadRevealComposer;

using System;
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

    // returns true if and only if the value in metadata equals the value passed in the arg
    // returns false otherwise
    public bool CheckValue(string key, string value)
    {
        string? realValue;
        if (_metadata.TryGetValue(key, out realValue))
        {
            if (realValue!.Equals(value, System.StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }

        Console.Error.WriteLine(
            $"ModelMetadata::CheckValue : Trying to retrieve a metadata attribute {key} that does not exist."
        );
        return false;
    }
}
