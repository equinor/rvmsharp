namespace CadRevealRvmProvider;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
public abstract record ProtoMesh(
    RvmPrimitive RvmPrimitive,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

public sealed record ProtoMeshFromFacetGroup(
    RvmFacetGroup FacetGroup,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    String Area
) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox, Area);

public sealed record ProtoMeshFromRvmPyramid(
    RvmPyramid Pyramid,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    String Area
) : ProtoMesh(Pyramid, TreeIndex, Color, AxisAlignedBoundingBox, Area);

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
