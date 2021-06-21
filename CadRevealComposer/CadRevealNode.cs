namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.Containers;
    using RvmSharp.Primitives;
    using System;

    public class CadRevealNode
    {
        public ulong NodeId;

        public ulong TreeIndex;

        // TODO support Store, Model, File and maybe not RVM
        public RvmGroup? Group; // PDMS inside, children inside
        public CadRevealNode? Parent;
        public CadRevealNode[]? Children;


        public APrimitive[] Geometries = Array.Empty<APrimitive>();
        public RvmPrimitive[] RvmGeometries = Array.Empty<RvmPrimitive>();

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
        [JsonProperty("header")] public Header Header { get; set; } = new Header();

        [JsonProperty("primitive_collections")]
        public PrimitiveCollections PrimitiveCollections { get; set; } = new PrimitiveCollections();
    }

    public class PrimitiveCollections
    {
        [JsonProperty("box_collection")] public Box[] BoxCollection = Array.Empty<Box>();
        [JsonProperty("circle_collection")] public Circle[] CircleCollection = Array.Empty<Circle>();

        [JsonProperty("closed_cone_collection")]
        public ClosedCone[] ClosedConeCollection = Array.Empty<ClosedCone>();

        [JsonProperty("closed_cylinder_collection")]
        public ClosedCylinder[] ClosedCylinderCollection = Array.Empty<ClosedCylinder>();

        [JsonProperty("closed_eccentric_cone_collection")]
        public ClosedEccentricCone[] ClosedEccentricConeCollection = Array.Empty<ClosedEccentricCone>();

        [JsonProperty("closed_ellipsoid_segment_collection")]
        public ClosedEllipsoidSegment[] ClosedEllipsoidSegmentCollection = Array.Empty<ClosedEllipsoidSegment>();

        [JsonProperty("closed_extruded_ring_segment_collection")]
        public ClosedExtrudedRingSegment[] ClosedExtrudedRingSegmentCollection = Array.Empty<ClosedExtrudedRingSegment>();

        [JsonProperty("closed_spherical_segment_collection")]
        public ClosedSphericalSegment[] ClosedSphericalSegmentCollection = Array.Empty<ClosedSphericalSegment>();

        [JsonProperty("closed_torus_segment_collection")]
        public ClosedTorusSegment[] ClosedTorusSegmentCollection = Array.Empty<ClosedTorusSegment>();

        [JsonProperty("ellipsoid_collection")] public Ellipsoid[] EllipsoidCollection = Array.Empty<Ellipsoid>();

        [JsonProperty("extruded_ring_collection")]
        public ExtrudedRing[] ExtrudedRingCollection = Array.Empty<ExtrudedRing>();

        [JsonProperty("nut_collection")] public Nut[] NutCollection = Array.Empty<Nut>();
        [JsonProperty("open_cone_collection")] public OpenCone[] OpenConeCollection = Array.Empty<OpenCone>();

        [JsonProperty("open_cylinder_collection")]
        public OpenCylinder[] OpenCylinderCollection = Array.Empty<OpenCylinder>();

        [JsonProperty("open_eccentric_cone_collection")]
        public OpenEccentricCone[] OpenEccentricConeCollection = Array.Empty<OpenEccentricCone>();

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
        public OpenGeneralCylinder[] OpenGeneralCylinderCollection = Array.Empty<OpenGeneralCylinder>();

        [JsonProperty("closed_general_cylinder_collection")]
        public ClosedGeneralCylinder[] ClosedGeneralCylinderCollection = Array.Empty<ClosedGeneralCylinder>();

        [JsonProperty("solid_open_general_cylinder_collection")]
        public SolidOpenGeneralCylinder[] SolidOpenGeneralCylinderCollection = Array.Empty<SolidOpenGeneralCylinder>();

        [JsonProperty("solid_closed_general_cylinder_collection")]
        public SolidClosedGeneralCylinder[] SolidClosedGeneralCylinderCollection = Array.Empty<SolidClosedGeneralCylinder>();

        [JsonProperty("open_general_cone_collection")]
        public OpenGeneralCone[] OpenGeneralConeCollection = Array.Empty<OpenGeneralCone>();

        [JsonProperty("closed_general_cone_collection")]
        public ClosedGeneralCone[] ClosedGeneralConeCollection = Array.Empty<ClosedGeneralCone>();

        [JsonProperty("solid_open_general_cone_collection")]
        public SolidOpenGeneralCone[] SolidOpenGeneralConeCollection = Array.Empty<SolidOpenGeneralCone>();

        [JsonProperty("solid_closed_general_cone_collection")]
        public SolidClosedGeneralCone[] SolidClosedGeneralConeCollection = Array.Empty<SolidClosedGeneralCone>();

        [JsonProperty("triangle_mesh_collection")]
        public TriangleMesh[] TriangleMeshCollection = Array.Empty<TriangleMesh>();

        [JsonProperty("instanced_mesh_collection")]
        public InstancedMesh[] InstancedMeshCollection = Array.Empty<InstancedMesh>();
    }


    public class Header
    {
        [JsonProperty("magic_bytes")] public uint MagicBytes { get; set; }

        [JsonProperty("format_version")] public uint FormatVersion { get; set; }

        [JsonProperty("optimizer_version")] public uint OptimizerVersion { get; set; }

        [JsonProperty("sector_id")] public uint SectorId { get; set; }

        [JsonProperty("parent_sector_id")] public long? ParentSectorId { get; set; } // FIXME this one is actually ulong, but JSON export requires -1 for no parent

        [JsonProperty("bbox_min")] public float[] BboxMin { get; set; } = {0f, 0f, 0f};

        [JsonProperty("bbox_max")] public float[] BboxMax { get; set; } = {0f, 0f, 0f};

        [JsonProperty("attributes")] public Attributes? Attributes { get; set; } = new();
    }

    public class Attributes
    {
        [JsonProperty("color")] public int[][] Color { get; set; } = Array.Empty<int[]>();

        [JsonProperty("diagonal")] public float[] Diagonal { get; set; }  = Array.Empty<float>();

        [JsonProperty("center_x")] public float[] CenterX { get; set; } = Array.Empty<float>();

        [JsonProperty("center_y")] public float[] CenterY { get; set; } = Array.Empty<float>();

        [JsonProperty("center_z")] public float[] CenterZ { get; set; } = Array.Empty<float>();

        [JsonProperty("normal")] public float[][] Normal { get; set; } = Array.Empty<float[]>();

        [JsonProperty("delta")] public float[] Delta { get; set; } = Array.Empty<float>();

        [JsonProperty("height")] public float[] Height { get; set; } = Array.Empty<float>();

        [JsonProperty("radius")] public float[] Radius { get; set; } = Array.Empty<float>();

        [JsonProperty("angle")] public float[] Angle { get; set; } = Array.Empty<float>();

        [JsonProperty("translation_x")] public float[] TranslationX { get; set; } = Array.Empty<float>();

        [JsonProperty("translation_y")] public float[] TranslationY { get; set; } = Array.Empty<float>();

        [JsonProperty("translation_z")] public float[] TranslationZ { get; set; } = Array.Empty<float>();

        [JsonProperty("scale_x")] public float[] ScaleX { get; set; } = Array.Empty<float>();

        [JsonProperty("scale_y")] public float[] ScaleY { get; set; } = Array.Empty<float>();

        [JsonProperty("scale_z")] public float[] ScaleZ { get; set; } = Array.Empty<float>();

        [JsonProperty("file_id")] public ulong[] FileId { get; set; } = Array.Empty<ulong>();

        [JsonProperty("texture")] public TriangleMesh.Texture[] Texture { get; set; } = Array.Empty<TriangleMesh.Texture>();
    }
}