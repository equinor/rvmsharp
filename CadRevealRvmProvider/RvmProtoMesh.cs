namespace CadRevealRvmProvider;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations;
using CadRevealComposer.Operations.SectorSplitting;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
public abstract record ProtoMesh(
    RvmPrimitive RvmPrimitive,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority Priority
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Priority);

public sealed record ProtoMeshFromFacetGroup(
    RvmFacetGroup FacetGroup,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority Priority
) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox, Priority);

public sealed record ProtoMeshFromRvmPyramid(
    RvmPyramid Pyramid,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority Priority
) : ProtoMesh(Pyramid, TreeIndex, Color, AxisAlignedBoundingBox, Priority);

/// <summary>
/// Sole purpose is to keep the <see cref="ProtoMeshFromFacetGroup"/> through processing of facet group instancing.
/// </summary>
public record RvmFacetGroupWithProtoMesh(
    ProtoMeshFromFacetGroup ProtoMesh,
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    RvmFacetGroup.RvmPolygon[] Polygons
) : RvmFacetGroup(Version, Matrix, BoundingBoxLocal, Polygons);
