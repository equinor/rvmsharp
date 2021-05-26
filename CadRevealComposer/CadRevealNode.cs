namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.Containers;
    using System;
    using System.Collections.Generic;

    public class CadRevealNode
    {
        public ulong NodeId;
        public ulong TreeIndex;
        // TODO support Store, Model, File and maybe not RVM
        public RvmGroup? Group; // PDMS inside, children inside
        public CadRevealNode? Parent;
        public CadRevealNode[]? Children;

        public APrimitive[] Geometries = Array.Empty<APrimitive>();
        // Bounding box
        // Depth
        // Subtree size
    }


    public partial class FileI3D
    {
        [JsonProperty("FileSector")]
        public FileSector FileSector { get; set; }
    }

    public partial class FileSector
    {
        [JsonProperty("header")]
        public Header Header { get; set; }

        [JsonProperty("primitive_collections")]
        public Dictionary<string, APrimitive[]> PrimitiveCollections { get; set; } = new Dictionary<string, APrimitive[]>();
    }

    public partial class Header
    {
        [JsonProperty("magic_bytes")]
        public long MagicBytes { get; set; }

        [JsonProperty("format_version")]
        public long FormatVersion { get; set; }

        [JsonProperty("optimizer_version")]
        public long OptimizerVersion { get; set; }

        [JsonProperty("sector_id")]
        public long SectorId { get; set; }

        [JsonProperty("parent_sector_id")]
        public long? ParentSectorId { get; set; }

        [JsonProperty("bbox_min")]
        public double[] BboxMin { get; set; }

        [JsonProperty("bbox_max")]
        public double[] BboxMax { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }
    }

    public partial class Attributes
    {
        [JsonProperty("color")]
        public int[][] Color { get; set; }

        [JsonProperty("diagonal")]
        public float[] Diagonal { get; set; }

        [JsonProperty("center_x")]
        public float[] CenterX { get; set; }

        [JsonProperty("center_y")]
        public float[] CenterY { get; set; }

        [JsonProperty("center_z")]
        public float[] CenterZ { get; set; }

        [JsonProperty("normal")]
        public float[][] Normal { get; set; }

        [JsonProperty("delta")]
        public float[] Delta { get; set; }

        [JsonProperty("height")]
        public float[] Height { get; set; }

        [JsonProperty("radius")]
        public float[] Radius { get; set; }

        [JsonProperty("angle")]
        public float[] Angle { get; set; }

        [JsonProperty("translation_x")]
        public object[] TranslationX { get; set; }

        [JsonProperty("translation_y")]
        public object[] TranslationY { get; set; }

        [JsonProperty("translation_z")]
        public object[] TranslationZ { get; set; }

        [JsonProperty("scale_x")]
        public object[] ScaleX { get; set; }

        [JsonProperty("scale_y")]
        public object[] ScaleY { get; set; }

        [JsonProperty("scale_z")]
        public object[] ScaleZ { get; set; }

        [JsonProperty("file_id")]
        public object[] FileId { get; set; }

        [JsonProperty("texture")]
        public object[] Texture { get; set; }
    }
}
