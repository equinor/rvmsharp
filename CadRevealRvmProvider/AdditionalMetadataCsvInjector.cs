namespace CadRevealRvmProvider;

using CadRevealComposer;
using CadRevealComposer.Utils;

public static class AdditionalMetadataCsvInjector
{
    public static void AddCustomEchoAttributeMetadata(
        IReadOnlyList<CadRevealNode> nodes,
        FileInfo additionalMetadataFile
    )
    {
        var metadataByRefNo = LoadAdditionalMetadataFromCsv(additionalMetadataFile);
        if (metadataByRefNo == null)
            return;

        var matchedEquipmentNodes = ApplyMetadataToNodes(nodes, metadataByRefNo);

        if (matchedEquipmentNodes == 0)
            return;

        Console.WriteLine(
            $"Added metadata for {matchedEquipmentNodes} out of {metadataByRefNo.Count} nodes based on RefNo matching in file {additionalMetadataFile.Name}."
        );
    }

    /// <summary>
    /// Loads additional metadata from a CSV file.
    /// </summary>
    /// <param name="additionalMetadataCsv">The CSV file containing additional metadata with a "RefNo" column.</param>
    /// <returns>
    /// A dictionary mapping each RefNo to its additional attributes (excluding RefNo itself),
    /// or null if the file is empty or missing the RefNo column.
    /// </returns>
    public static Dictionary<string, Dictionary<string, string>>? LoadAdditionalMetadataFromCsv(
        FileInfo additionalMetadataCsv
    )
    {
        using var reader = new StreamReader(additionalMetadataCsv.FullName);
        var header = reader.ReadLine();
        if (header == null)
            return null;

        var headers = header.Split(',');
        var refNoIndex = Array.IndexOf(headers, "RefNo");

        if (refNoIndex == -1)
            return null;

        var result = new Dictionary<string, Dictionary<string, string>>();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (line == null)
                continue;
            var values = line.Split(',');
            if (values.Length <= refNoIndex)
                continue;

            var refNo = values[refNoIndex];
            if (string.IsNullOrWhiteSpace(refNo))
                continue;

            // Map all the attributes except the RefNo column
            var attrs = headers
                .Select((h, i) => (h, i))
                .Where(x => x.i != refNoIndex && x.i < values.Length)
                .ToDictionary(x => x.h, x => values[x.i]);
            result[refNo] = attrs;
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// Applies equipment metadata to nodes that have a matching RefNo attribute.
    /// </summary>
    /// <param name="nodes">The nodes to which metadata will be applied.</param>
    /// <param name="metadataByRefNo">Maps each RefNo to its additional attributes.</param>
    /// <returns>The number of nodes to which metadata was successfully applied.</returns>
    public static int ApplyMetadataToNodes(
        IReadOnlyList<CadRevealNode> nodes,
        Dictionary<string, Dictionary<string, string>> metadataByRefNo
    )
    {
        var matched = 0;

        foreach (var node in nodes)
        {
            var refNo = node.Attributes.GetValueOrNull("RefNo");
            if (refNo == null || !metadataByRefNo.TryGetValue(refNo, out var attrs))
                continue;

            foreach ((string key, string value) in attrs)
            {
                var injectedAttributeKey = $"EchoInjected_{key}";
                if (node.Attributes.ContainsKey(injectedAttributeKey))
                    Console.WriteLine(
                        $"Warning: Overwriting previously injected attribute '{injectedAttributeKey}' for node '{node.Name}' with RefNo '{refNo}'. This may not be intentional!"
                    );
                node.Attributes[injectedAttributeKey] = "true";

                if (node.Attributes.TryGetValue(key, out var originalValue))
                    node.Attributes.TryAdd(key + "_Original", originalValue); // Uses TryAdd to avoid overwriting existing original value if multiple overrides exist on same part.

                node.Attributes[key] = value;
            }
            matched++;
        }

        return matched;
    }
}
