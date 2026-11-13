namespace CadRevealRvmProvider;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record TagMetadataEntry(
    [property: JsonPropertyName("TAG_NO")] string? TagNo,
    [property: JsonPropertyName("TAG_CATEGORY")] int? CategoryId,
    [property: JsonPropertyName("TAG_CATEGORY_DESCRIPTION")] string? CategoryDescription,
    [property: JsonPropertyName("PROJECT_CODE")] string? ProjectCode,
    [property: JsonPropertyName("SYSTEM")] string? System
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
