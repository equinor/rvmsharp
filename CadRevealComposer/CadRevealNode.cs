namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.Containers;
    using RvmSharp.Primitives;
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

        /// <summary>
        /// This is a bounding box encapsulating all childrens bounding boxes.
        /// Some nodes are "Notes", and can validly not have any Bounds
        /// </summary>
        public RvmBoundingBox? BoundingBoxAxisAligned;
        // Depth
        // Subtree size
    }


    public class FileI3D
    {
        [JsonProperty("FileSector")] public FileSector? FileSector { get; set; }
    }

    public class FileSector
    {
        [JsonProperty("header")] public Header? Header { get; set; }

        [JsonProperty("primitive_collections")]
        public PrimitiveCollections PrimitiveCollections { get; set; } = new PrimitiveCollections();
    }

    public class PrimitiveCollections
    {
        [JsonProperty("box_collection")] public Box[] BoxCollection = Array.Empty<Box>();
        [JsonProperty("circle_collection")] public APrimitive[] CircleCollection = Array.Empty<APrimitive>();

        [JsonProperty("closed_cone_collection")]
        public ClosedCone[] ClosedConeCollection = Array.Empty<ClosedCone>();

        [JsonProperty("closed_cylinder_collection")]
        public ClosedCylinder[] ClosedCylinderCollection = Array.Empty<ClosedCylinder>();

        [JsonProperty("closed_eccentric_cone_collection")]
        public APrimitive[] ClosedEccentricConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("closed_ellipsoid_segment_collection")]
        public ClosedEllipsoidSegment[] ClosedEllipsoidSegmentCollection = Array.Empty<ClosedEllipsoidSegment>();

        [JsonProperty("closed_extruded_ring_segment_collection")]
        public ClosedExtrudedRingSegment[] ClosedExtrudedRingSegmentCollection = Array.Empty<ClosedExtrudedRingSegment>();

        [JsonProperty("closed_spherical_segment_collection")]
        public ClosedSphericalSegment[] ClosedSphericalSegmentCollection = Array.Empty<ClosedSphericalSegment>();

        [JsonProperty("closed_torus_segment_collection")]
        public ClosedTorusSegment[] ClosedTorusSegmentCollection = Array.Empty<ClosedTorusSegment>();

        [JsonProperty("ellipsoid_collection")] public APrimitive[] EllipsoidCollection = Array.Empty<APrimitive>();

        [JsonProperty("extruded_ring_collection")]
        public ExtrudedRing[] ExtrudedRingCollection = Array.Empty<ExtrudedRing>();

        [JsonProperty("nut_collection")] public APrimitive[] NutCollection = Array.Empty<APrimitive>();
        [JsonProperty("open_cone_collection")] public OpenCone[] OpenConeCollection = Array.Empty<OpenCone>();

        [JsonProperty("open_cylinder_collection")]
        public OpenCylinder[] OpenCylinderCollection = Array.Empty<OpenCylinder>();

        [JsonProperty("open_eccentric_cone_collection")]
        public APrimitive[] OpenEccentricConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("open_ellipsoid_segment_collection")]
        public OpenEllipsoidSegment[] OpenEllipsoidSegmentCollection = Array.Empty<OpenEllipsoidSegment>();

        [JsonProperty("open_extruded_ring_segment_collection")]
        public OpenExtrudedRingSegment[] OpenExtrudedRingSegmentCollection = Array.Empty<OpenExtrudedRingSegment>();

        [JsonProperty("open_spherical_segment_collection")]
        public OpenSphericalSegment[] OpenSphericalSegmentCollection = Array.Empty<OpenSphericalSegment>();

        [JsonProperty("open_torus_segment_collection")]
        public OpenTorusSegment[] OpenTorusSegmentCollection = Array.Empty<OpenTorusSegment>();

        [JsonProperty("ring_collection")] public Ring[] RingCollection = Array.Empty<Ring>();
        [JsonProperty("sphere_collection")] public Sphere[] SphereCollection = Array.Empty<Sphere>();
        [JsonProperty("torus_collection")] public Torus[] TorusCollection = Array.Empty<Torus>();

        [JsonProperty("open_general_cylinder_collection")]
        public APrimitive[] OpenGeneralCylinderCollection = Array.Empty<APrimitive>();

        [JsonProperty("closed_general_cylinder_collection")]
        public APrimitive[] ClosedGeneralCylinderCollection = Array.Empty<APrimitive>();

        [JsonProperty("solid_open_general_cylinder_collection")]
        public APrimitive[] SolidOpenGeneralCylinderCollection = Array.Empty<APrimitive>();

        [JsonProperty("solid_closed_general_cylinder_collection")]
        public APrimitive[] SolidClosedGeneralCylinderCollection = Array.Empty<APrimitive>();

        [JsonProperty("open_general_cone_collection")]
        public APrimitive[] OpenGeneralConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("closed_general_cone_collection")]
        public APrimitive[] ClosedGeneralConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("solid_open_general_cone_collection")]
        public APrimitive[] SolidOpenGeneralConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("solid_closed_general_cone_collection")]
        public APrimitive[] SolidClosedGeneralConeCollection = Array.Empty<APrimitive>();

        [JsonProperty("triangle_mesh_collection")]
        public APrimitive[] TriangleMeshCollection = Array.Empty<APrimitive>();

        [JsonProperty("instanced_mesh_collection")]
        public APrimitive[] InstancedMeshCollection = Array.Empty<APrimitive>();
    }


    public class Header
    {
        [JsonProperty("magic_bytes")] public long MagicBytes { get; set; }

        [JsonProperty("format_version")] public long FormatVersion { get; set; }

        [JsonProperty("optimizer_version")] public long OptimizerVersion { get; set; }

        [JsonProperty("sector_id")] public long SectorId { get; set; }

        [JsonProperty("parent_sector_id")] public long? ParentSectorId { get; set; }

        [JsonProperty("bbox_min")] public float[]? BboxMin { get; set; }

        [JsonProperty("bbox_max")] public float[]? BboxMax { get; set; }

        [JsonProperty("attributes")] public Attributes Attributes { get; set; } = new Attributes();
    }

    public class Attributes
    {
        [JsonProperty("color")] public int[][]? Color { get; set; }

        [JsonProperty("diagonal")] public float[]? Diagonal { get; set; }

        [JsonProperty("center_x")] public float[]? CenterX { get; set; }

        [JsonProperty("center_y")] public float[]? CenterY { get; set; }

        [JsonProperty("center_z")] public float[]? CenterZ { get; set; }

        [JsonProperty("normal")] public float[][]? Normal { get; set; }

        [JsonProperty("delta")] public float[]? Delta { get; set; }

        [JsonProperty("height")] public float[]? Height { get; set; }

        [JsonProperty("radius")] public float[]? Radius { get; set; }

        [JsonProperty("angle")] public float[]? Angle { get; set; }

        [JsonProperty("translation_x")] public object[]? TranslationX { get; set; }

        [JsonProperty("translation_y")] public object[]? TranslationY { get; set; }

        [JsonProperty("translation_z")] public object[]? TranslationZ { get; set; }

        [JsonProperty("scale_x")] public object[]? ScaleX { get; set; }

        [JsonProperty("scale_y")] public object[]? ScaleY { get; set; }

        [JsonProperty("scale_z")] public object[]? ScaleZ { get; set; }

        [JsonProperty("file_id")] public object[]? FileId { get; set; }

        [JsonProperty("texture")] public object[]? Texture { get; set; }
    }
}