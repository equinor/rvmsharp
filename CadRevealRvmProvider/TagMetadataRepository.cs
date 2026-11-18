namespace CadRevealRvmProvider;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record TagMetadataEntry(
    [property: JsonPropertyName("TAG_NO")] string? TagNo,
    [property: JsonPropertyName("TAG_CATEGORY")] int? CategoryId,
    [property: JsonPropertyName("TAG_CATEGORY_DESCRIPTION")] string? CategoryDescription,
    [property: JsonPropertyName("SYSTEM")] string? System,
    [property: JsonPropertyName("TAG_STATUS")] string? TagStatus = null
);

public sealed class TagMetadataRepository
{
    private sealed record TagReport(int SchemaVersion, string? ReportName, TagMetadataEntry?[]? Rows);

    private readonly Dictionary<string, TagMetadataEntry> _entries;

    private TagMetadataRepository(Dictionary<string, TagMetadataEntry> entries)
    {
        _entries = entries;
    }

    public int Count => _entries.Count;

    public bool TryGetValue(string tag, [NotNullWhen(true)] out TagMetadataEntry? entry)
    {
        return _entries.TryGetValue(tag, out entry);
    }

    public IReadOnlyList<TagMetadataEntry> FindMatches(string tag)
    {
        var leadingWildcard = tag.StartsWith('*');
        var trailingWildcard = tag.EndsWith('*');

        // not a wildcard, matching allowed for all categories
        if (!leadingWildcard && !trailingWildcard)
            return TryGetValue(tag, out var entry) ? [entry] : [];

        // matching of wildcards in only allowed for category 6, i.e. type "LINE"

        var start = leadingWildcard ? 1 : 0;
        var end = trailingWildcard ? tag.Length - 1 : tag.Length;
        var tagCoreToMatch = tag.Substring(start, Math.Max(0, end - start));

        var matches = new List<TagMetadataEntry>();
        foreach (var (repositoryTag, candidate) in _entries)
        {
            if (candidate.CategoryId != 6)
                continue;

            var isMatch =
                leadingWildcard && trailingWildcard ? repositoryTag.Contains(tagCoreToMatch, StringComparison.Ordinal)
                : leadingWildcard ? repositoryTag.EndsWith(tagCoreToMatch, StringComparison.Ordinal)
                : repositoryTag.StartsWith(tagCoreToMatch, StringComparison.Ordinal);
            if (isMatch)
                matches.Add(candidate);
        }

        // TODO: check if this is OK
        // if there are any A (asb) tags, then we remove other tags (H, historic, probably).
        // OBS: Did not check if there are alternative statuses than A and H
        if (matches.Any(m => m.TagStatus == "A"))
            matches = matches.Where(m => m.TagStatus == "A").ToList();

        return matches;
    }

    public static TagMetadataRepository Load(FileInfo file)
    {
        try
        {
            using var stream = file.OpenRead();
            var report = JsonSerializer.Deserialize<TagReport>(stream);

            // CHECK: Not sure if SchemaVersion should be fixed
            if (report is not { SchemaVersion: 1, ReportName: "TAG_IN_3D", Rows: not null })
                throw new JsonException("Expected SchemaVersion 1, ReportName 'TAG_IN_3D', and a Rows array.");

            var entries = new Dictionary<string, TagMetadataEntry>(StringComparer.Ordinal);
            for (var i = 0; i < report.Rows.Length; i++)
            {
                var entry = report.Rows[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.TagNo))
                    throw new JsonException($"Row {i + 1} must contain a TAG_NO.");

                // trying to insert a row (with different values) with the same TAG_NO
                if (!entries.TryAdd(entry.TagNo, entry) && entries[entry.TagNo] != entry)
                    throw new JsonException($"Conflicting metadata for TAG_NO '{entry.TagNo}' at row {i + 1}.");
            }

            return new TagMetadataRepository(entries);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid tag repository '{file.FullName}': {exception.Message}", exception);
        }
    }
}
