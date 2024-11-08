namespace CadRevealRvmProvider;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
public abstract record ProtoMesh(
    RvmPrimitive RvmPrimitive,
    uint TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, 0, null);

public sealed record ProtoMeshFromFacetGroup(
    RvmFacetGroup FacetGroup,
    uint TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox
) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record ProtoMeshFromRvmPyramid(
    RvmPyramid Pyramid,
    uint TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox
) : ProtoMesh(Pyramid, TreeIndex, Color, AxisAlignedBoundingBox);

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
