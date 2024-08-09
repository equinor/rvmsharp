namespace CadRevealComposer;

using System;
using System.Numerics;
using System.Text.Json.Serialization;
using Configuration;
using Operations.SectorSplitting;
using Utils;

public class Scene
{
    [JsonPropertyName("version")]
    public long Version { get; init; }

    [JsonPropertyName("projectId")]
    private long ProjectIdJson => ProjectId.Value;

    [JsonIgnore]
    public ProjectId ProjectId { get; init; } = new ProjectId(0);

    [JsonPropertyName("modelId")]
    public long ModelIdJson => ModelId.Value;

    [JsonIgnore]
    public ModelId ModelId { get; init; } = new ModelId(0);

    [JsonPropertyName("revisionId")]
    public long RevisionIdJson => RevisionId.Value;

    [JsonIgnore]
    public RevisionId RevisionId { get; init; } = new RevisionId(0);

    [JsonPropertyName("subRevisionId")]
    public long SubRevisionId { get; set; }

    [JsonPropertyName("maxTreeIndex")]
    public ulong MaxTreeIndex { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "Meters";

    [JsonPropertyName("sectors")]
    public Sector[] Sectors { get; set; } = Array.Empty<Sector>();
}

public class Sector
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("parentId")]
    public long? ParentId { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("depth")]
    public long Depth { get; set; }

    [JsonPropertyName("estimatedDrawCallCount")]
    public long EstimatedDrawCallCount { get; set; }

    [JsonPropertyName("estimatedTriangleCount")]
    public long EstimatedTriangleCount { get; set; }

    /// <summary>
    /// Bounding box which includes the sector's own geometry and all children's geometry
    /// </summary>
    [JsonPropertyName("boundingBox")]
    public SerializableBoundingBox SubtreeBoundingBox { get; set; } = null!;

    /// <summary>
    /// Bounding box which includes the sector's own geometry
    /// </summary>
    [JsonPropertyName("geometryBoundingBox")]
    public SerializableBoundingBox? GeometryBoundingBox { get; set; }

    #region GltfSceneSectorMetadata

    [JsonPropertyName("sectorFileName")]
    public string? SectorFileName { get; set; } = null;

    [JsonPropertyName("minDiagonalLength")]
    public float MinDiagonalLength { get; set; } = 1;

    [JsonPropertyName("maxDiagonalLength")]
    public float MaxDiagonalLength { get; set; } = 1;

    [JsonPropertyName("downloadSize")]
    public long DownloadSize { get; set; }

    #endregion

    /// <summary>
    /// Stores echo developer metadata, not used by reveal but may be used by Echo in dev.
    /// NOT reliable, if we want data to be used in algorithms move them somewhere else.
    /// </summary>
    [JsonPropertyName("sectorEchoDevMetadata")]
    public SectorEchoDevMetadata? SectorEchoDevMetadata { get; set; } = null;
}

public class SectorEchoDevMetadata
{
    public GeometryDistributionStats? GeometryDistributions { get; set; } = null;
    public SectorSplittingMetadata? SplittingStats { get; set; } = null;
}

public record SerializableBoundingBox(
    [property: JsonPropertyName("min")] SerializableVector3 Min,
    [property: JsonPropertyName("max")] SerializableVector3 Max
);

[JsonSerializable(typeof(SerializableVector3))]
public record SerializableVector3(
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("z")] float Z
)
{
    public static SerializableVector3 FromVector3(Vector3 vector3)
    {
        return new SerializableVector3(vector3.X, vector3.Y, vector3.Z);
    }
};
