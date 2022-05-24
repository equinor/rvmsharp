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

    [JsonProperty("parentId")] public long ParentId { get; set; }

    [JsonProperty("path")] public string Path { get; set; } = "";

    [JsonProperty("depth")] public long Depth { get; set; }

    [JsonProperty("boundingBox")] public BoundingBox BoundingBox { get; set; } = null!;

    [JsonProperty("indexFile")] public IndexFile IndexFile { get; set; } = null!;

    [JsonProperty("facesFile")] public FacesFile? FacesFile { get; set; }

    [JsonProperty("estimatedTriangleCount")]
    public long EstimatedTriangleCount { get; set; }

    [JsonProperty("estimatedDrawCallCount")]
    public long EstimatedDrawCallCount { get; set; }
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

public record CoverageFactors(
    [property: JsonProperty("yz")] float Yz,
    [property: JsonProperty("xz")] float Xz,
    [property: JsonProperty("xy")] float Xy);

public record IndexFile(
    [property: JsonProperty("fileName")] string FileName,
    [property: JsonProperty("downloadSize")]
    long DownloadSize,
    [property: JsonProperty("peripheralFiles")]
    string[] PeripheralFiles);

public record FacesFile(
    [property: JsonProperty("fileName")] string? FileName,
    [property: JsonProperty("downloadSize")]
    long DownloadSize,
    [property: JsonProperty("quadSize")] float QuadSize,
    [property: JsonProperty("coverageFactors")]
    CoverageFactors CoverageFactors,
    [property: JsonProperty("recursiveCoverageFactors")]
    CoverageFactors? RecursiveCoverageFactors);