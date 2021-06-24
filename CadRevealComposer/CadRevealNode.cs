namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.Containers;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Numerics;

    public class CadRevealNode
    {
        public ulong NodeId;

        public ulong TreeIndex;

        // TODO support Store, Model, File and maybe not RVM
        public RvmGroup? Group; // PDMS inside, children inside
        public CadRevealNode? Parent;
        public CadRevealNode[]? Children;

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
        public FileSector? FileSector { get; set; }
    }

    public class FileSector
    {
        public Header Header { get; set; } = new Header();


        public PrimitiveCollections PrimitiveCollections { get; set; } = new PrimitiveCollections();
    }

    public class PrimitiveCollections
    {
        public Box[] BoxCollection = Array.Empty<Box>();
        public Circle[] CircleCollection = Array.Empty<Circle>();


        public ClosedCone[] ClosedConeCollection = Array.Empty<ClosedCone>();


        public ClosedCylinder[] ClosedCylinderCollection = Array.Empty<ClosedCylinder>();


        public ClosedEccentricCone[] ClosedEccentricConeCollection = Array.Empty<ClosedEccentricCone>();


        public ClosedEllipsoidSegment[] ClosedEllipsoidSegmentCollection = Array.Empty<ClosedEllipsoidSegment>();


        public ClosedExtrudedRingSegment[] ClosedExtrudedRingSegmentCollection = Array.Empty<ClosedExtrudedRingSegment>();


        public ClosedSphericalSegment[] ClosedSphericalSegmentCollection = Array.Empty<ClosedSphericalSegment>();


        public ClosedTorusSegment[] ClosedTorusSegmentCollection = Array.Empty<ClosedTorusSegment>();

        public Ellipsoid[] EllipsoidCollection = Array.Empty<Ellipsoid>();


        public ExtrudedRing[] ExtrudedRingCollection = Array.Empty<ExtrudedRing>();

        public Nut[] NutCollection = Array.Empty<Nut>();
        public OpenCone[] OpenConeCollection = Array.Empty<OpenCone>();


        public OpenCylinder[] OpenCylinderCollection = Array.Empty<OpenCylinder>();


        public OpenEccentricCone[] OpenEccentricConeCollection = Array.Empty<OpenEccentricCone>();


        public OpenEllipsoidSegment[] OpenEllipsoidSegmentCollection = Array.Empty<OpenEllipsoidSegment>();


        public OpenExtrudedRingSegment[] OpenExtrudedRingSegmentCollection = Array.Empty<OpenExtrudedRingSegment>();


        public OpenSphericalSegment[] OpenSphericalSegmentCollection = Array.Empty<OpenSphericalSegment>();


        public OpenTorusSegment[] OpenTorusSegmentCollection = Array.Empty<OpenTorusSegment>();

        public Ring[] RingCollection = Array.Empty<Ring>();
        public Sphere[] SphereCollection = Array.Empty<Sphere>();
        public Torus[] TorusCollection = Array.Empty<Torus>();


        public OpenGeneralCylinder[] OpenGeneralCylinderCollection = Array.Empty<OpenGeneralCylinder>();


        public ClosedGeneralCylinder[] ClosedGeneralCylinderCollection = Array.Empty<ClosedGeneralCylinder>();


        public SolidOpenGeneralCylinder[] SolidOpenGeneralCylinderCollection = Array.Empty<SolidOpenGeneralCylinder>();


        public SolidClosedGeneralCylinder[] SolidClosedGeneralCylinderCollection = Array.Empty<SolidClosedGeneralCylinder>();


        public OpenGeneralCone[] OpenGeneralConeCollection = Array.Empty<OpenGeneralCone>();


        public ClosedGeneralCone[] ClosedGeneralConeCollection = Array.Empty<ClosedGeneralCone>();


        public SolidOpenGeneralCone[] SolidOpenGeneralConeCollection = Array.Empty<SolidOpenGeneralCone>();


        public SolidClosedGeneralCone[] SolidClosedGeneralConeCollection = Array.Empty<SolidClosedGeneralCone>();


        public TriangleMesh[] TriangleMeshCollection = Array.Empty<TriangleMesh>();


        public InstancedMesh[] InstancedMeshCollection = Array.Empty<InstancedMesh>();
    }


    public class Header
    {
        public uint MagicBytes { get; set; }

        public uint FormatVersion { get; set; }

        public uint OptimizerVersion { get; set; }

        public uint SectorId { get; set; }

        public long? ParentSectorId { get; set; } // FIXME this one is actually ulong, but JSON export requires -1 for no parent

        public Vector3 BboxMin { get; set; } = Vector3.Zero;

        public Vector3 BboxMax { get; set; } = Vector3.Zero;

        public Attributes? Attributes { get; set; } = new Attributes();
    }

    public class Attributes
    {
        public ImmutableSortedSet<Color> Color { get; set; } = ImmutableSortedSet<Color>.Empty;

        public ImmutableSortedSet<float> Diagonal { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> CenterX { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> CenterY { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> CenterZ { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<Vector3> Normal { get; set; } = ImmutableSortedSet<Vector3>.Empty;

        public ImmutableSortedSet<float> Delta { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> Height { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> Radius { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> Angle { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> TranslationX { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> TranslationY { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> TranslationZ { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> ScaleX { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> ScaleY { get; set; } = ImmutableSortedSet<float>.Empty;

        public ImmutableSortedSet<float> ScaleZ { get; set; } = ImmutableSortedSet<float>.Empty;

        public ulong[] FileId { get; set; } = Array.Empty<ulong>();

        public Texture[] Texture { get; set; } = Array.Empty<Texture>();
    }
}