namespace CadRevealRvmProvider;

using System.Globalization;
using CadRevealComposer;

public sealed record TagEnrichmentSummary(
    int MatchedNodes,
    int EnrichedNodes,
    int UnmatchedNodes,
    int RemovedTags,
    int SkippedExistingKeys
);

public static class TagMetadataInjector
{
    // CHECK: allows enriching from multiple json files, even though there may be only one per plant
    public static TagEnrichmentSummary? EnrichFromFiles(IReadOnlyList<CadRevealNode> nodes, IEnumerable<FileInfo> files)
    {
        // CHECK: not sure if the file will have _TAG_IN_3D_ in its name...
        var repositories = files
            .Where(file =>
                file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileNameWithoutExtension(file.Name)
                    .Contains("_TAG_IN_3D_", StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();
        if (repositories.Length == 0)
            return null;
        if (repositories.Length > 1)
            throw new InvalidDataException(
                "Expected at most one *_TAG_IN_3D_*.json tag repository, but found: "
                    + string.Join(", ", repositories.Select(file => file.FullName))
            );

        // CHECK: bit ugly to load it as repositories[0]
        var repository = TagMetadataRepository.Load(repositories[0]);
        var summary = ApplyMetadataToNodes(nodes, repository);
        Console.WriteLine(
            $"Tag repository '{repositories[0].Name}' ({repository.Count} tags): "
                + $"matched {summary.MatchedNodes} nodes, enriched {summary.EnrichedNodes}, "
                + $"unmatched {summary.UnmatchedNodes}, removed {summary.RemovedTags} invalid Tags, "
                + $"preserved {summary.SkippedExistingKeys} existing metadata values."
        );
        return summary;
    }

    public static TagEnrichmentSummary ApplyMetadataToNodes(
        IReadOnlyList<CadRevealNode> nodes,
        TagMetadataRepository repository
    )
    {
        var matched = 0;
        var enriched = 0;
        var removed = 0;
        var skipped = 0;
        foreach (var node in nodes)
        {
            if (!node.Attributes.TryGetValue("Tag", out var tag))
                continue;

            // For now keep the original Tag value as PdmsTag so we can debug
            node.Attributes.Add("PdmsTag", tag); // intentionally throws if the key already exists for some reason

            var matches = string.IsNullOrWhiteSpace(tag)
                ? Array.Empty<TagMetadataEntry>()
                : repository.FindMatches(tag);
            if (matches.Count == 0)
            {
                node.Attributes.Remove("Tag");
                removed++;
                continue;
            }

            matched++;
            var entry = matches[0];
            if (
                matches.Count != 1
                && matches.Any(candidate =>
                    candidate.CategoryId != entry.CategoryId
                    || candidate.CategoryDescription != entry.CategoryDescription
                    || candidate.System != entry.System
                // || candidate.TagStatus != entry.TagStatus -- removing status from comparison for now
                )
            )
                continue;

            var added = false;
            foreach (var (key, value) in GetAttributes(entry))
            {
                if (value == null)
                    continue;
                if (node.Attributes.TryAdd(key, value))
                    added = true;
                else
                    skipped++;
            }
            if (added)
                enriched++;
        }

        return new TagEnrichmentSummary(matched, enriched, removed, removed, skipped);
    }

    private static IEnumerable<(string Key, string? Value)> GetAttributes(TagMetadataEntry entry)
    {
        yield return ("TagMetadata.CategoryId", entry.CategoryId?.ToString(CultureInfo.InvariantCulture));
        yield return ("TagMetadata.CategoryDescription", entry.CategoryDescription);
        yield return ("TagMetadata.System", entry.System);
    }
}
