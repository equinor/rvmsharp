namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using System;

    public class Scene
    {
        [JsonProperty("version")]
        public long Version { get; set; }

        [JsonProperty("projectId")]
        public long ProjectId { get; set; }

        [JsonProperty("modelId")]
        public long ModelId { get; set; }

        [JsonProperty("revisionId")]
        public long RevisionId { get; set; }

        [JsonProperty("subRevisionId")]
        public long SubRevisionId { get; set; }

        [JsonProperty("maxTreeIndex")]
        public ulong MaxTreeIndex { get; set; }

        [JsonProperty("unit")] public string Unit { get; set; } = "Meters";

        [JsonProperty("sectors")] public Sector[] Sectors { get; set; } = Array.Empty<Sector>();
    }

    public class Sector
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("parentId")]
        public long ParentId { get; set; }

        [JsonProperty("path")] public string Path { get; set; } = "";

        [JsonProperty("depth")]
        public long Depth { get; set; }

        [JsonProperty("boundingBox")] public BoundingBox BoundingBox { get; set; } = null!;

        [JsonProperty("indexFile")] public IndexFile IndexFile { get; set; } = null!;

        [JsonProperty("facesFile")]
        public FacesFile? FacesFile { get; set; }

        [JsonProperty("estimatedTriangleCount")]
        public long EstimatedTriangleCount { get; set; }

        [JsonProperty("estimatedDrawCallCount")]
        public long EstimatedDrawCallCount { get; set; }
    }

    public class BoundingBox
    {
        [JsonProperty("min")] public BbVector3 Min { get; set; } = null!;

        [JsonProperty("max")] public BbVector3 Max { get; set; } = null!;
    }

    public class BbVector3
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    public class FacesFile
    {
        [JsonProperty("quadSize")]
        public double QuadSize { get; set; }

        [JsonProperty("facesCount")]
        public long FacesCount { get; set; }

        [JsonProperty("recursiveCoverageFactors")]
        public CoverageFactors RecursiveCoverageFactors { get; set; } = null!;

        [JsonProperty("coverageFactors")] public CoverageFactors CoverageFactors { get; set; } = null!;

        [JsonProperty("fileName")] public string FileName { get; set; } = "";

        [JsonProperty("downloadSize")]
        public long DownloadSize { get; set; }
    }

    public class CoverageFactors
    {
        [JsonProperty("yz")]
        public double Yz { get; set; }

        [JsonProperty("xz")]
        public double Xz { get; set; }

        [JsonProperty("xy")]
        public double Xy { get; set; }
    }

    public class IndexFile
    {
        [JsonProperty("peripheralFiles")] public string[] PeripheralFiles { get; set; } = null!;

        [JsonProperty("fileName")]
        public string FileName { get; set; } = "";

        [JsonProperty("downloadSize")]
        public long DownloadSize { get; set; } = 1;
    }
}
