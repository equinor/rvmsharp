namespace CadRevealComposer;

using Configuration;
using Newtonsoft.Json;
using System;

public class Scene
{
    [JsonProperty("version")] public long Version { get; init; }

    [JsonProperty("projectId")] private long ProjectIdJson => ProjectId.Value;
    [JsonIgnore] public ProjectId ProjectId { get; init; } = new ProjectId(0);

    [JsonProperty("modelId")] public long ModelIdJson => ModelId.Value;
    [JsonIgnore] public ModelId ModelId { get; init; } = new ModelId(0);


    [JsonProperty("revisionId")] public long RevisionIdJson => RevisionId.Value;
    [JsonIgnore] public RevisionId RevisionId { get; init; } = new RevisionId(0);


    [JsonProperty("subRevisionId")] public long SubRevisionId { get; set; }

    [JsonProperty("maxTreeIndex")] public ulong MaxTreeIndex { get; set; }

    [JsonProperty("unit")] public string Unit { get; set; } = "Meters";

    [JsonProperty("sectors")] public Sector[] Sectors { get; set; } = Array.Empty<Sector>();
}

public class Sector
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("parentId")] public long? ParentId { get; set; }
    [JsonProperty("path")] public string Path { get; set; } = "";
    [JsonProperty("depth")] public long Depth { get; set; }

    [JsonProperty("estimatedDrawCallCount")]
    public long EstimatedDrawCallCount { get; set; }

    [JsonProperty("estimatedTriangleCount")]
    public long EstimatedTriangleCount { get; set; }

    [JsonProperty("boundingBox")] public BoundingBox SubtreeBoundingBox { get; set; } = null!;

    /// <summary>
    /// TODO: Figure out what GeometryBoundingBox is needed for.
    /// Semi optional since its gracefully handled in reveal to not use this field in v9
    /// </summary>
    [JsonProperty("geometryBoundingBox")]
    public BoundingBox? GeometryBoundingBox { get; set; }

    #region GltfSceneSectorMetadata

    [JsonProperty("sectorFileName")] public string? SectorFileName { get; set; } = null;
    [JsonProperty("minDiagonalLength")] public float MinDiagonalLength { get; set; } = 1;
    [JsonProperty("maxDiagonalLength")] public float MaxDiagonalLength { get; set; } = 1;
    [JsonProperty("downloadSize")] public long DownloadSize { get; set; }

    #endregion
}

public record BoundingBox(
    [property: JsonProperty("min")] BbVector3 Min,
    [property: JsonProperty("max")] BbVector3 Max
);

public record BbVector3
(
    [property: JsonProperty("x")] float X,
    [property: JsonProperty("y")] float Y,
    [property: JsonProperty("z")] float Z
);