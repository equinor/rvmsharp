namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using System;

    public class Scene
    {
         public long Version { get; init; }

         private long ProjectIdJson => ProjectId.Value;
        [JsonIgnore] public ProjectId ProjectId { get; init; } = new ProjectId(0);

         public long ModelIdJson => ModelId.Value;
        [JsonIgnore] public ModelId ModelId { get; init; } = new ModelId(0);


         public long RevisionIdJson => RevisionId.Value;
        [JsonIgnore] public RevisionId RevisionId { get; init; } = new RevisionId(0);


         public long SubRevisionId { get; set; }

         public ulong MaxTreeIndex { get; set; }

         public string Unit { get; set; } = "Meters";

         public Sector[] Sectors { get; set; } = Array.Empty<Sector>();
    }

    public class Sector
    {
         public long Id { get; set; }

         public long ParentId { get; set; }

         public string Path { get; set; } = "";

         public long Depth { get; set; }

         public BoundingBox BoundingBox { get; set; } = null!;

         public IndexFile IndexFile { get; set; } = null!;

         public FacesFile? FacesFile { get; set; }


        public long EstimatedTriangleCount { get; set; }


        public long EstimatedDrawCallCount { get; set; }
    }

    public record BoundingBox(
         BbVector3 Min,
         BbVector3 Max
    );

    public record BbVector3
    (
         double X,
         double Y,
         double Z
    );

    public class FacesFile
    {
         public double QuadSize { get; set; }

         public long FacesCount { get; set; }


        public CoverageFactors RecursiveCoverageFactors { get; set; } = null!;

         public CoverageFactors CoverageFactors { get; set; } = null!;

         public string FileName { get; set; } = "";

         public long DownloadSize { get; set; }
    }

    public class CoverageFactors
    {
         public double Yz { get; set; }

         public double Xz { get; set; }

         public double Xy { get; set; }
    }

    public record IndexFile(
         string FileName,

        long DownloadSize,

        string[] PeripheralFiles);
}