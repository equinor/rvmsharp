namespace CadRevealRvmProvider;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using ProtoBuf;
using RvmSharp.Primitives;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
[ProtoContract(SkipConstructor = true)]
[ProtoInclude(1000, typeof(ProtoMeshFromFacetGroup))]
[ProtoInclude(1001, typeof(ProtoMeshFromRvmPyramid))]
public abstract record ProtoMesh(
    [property: ProtoIgnore] RvmPrimitive RvmPrimitive,
    uint TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, 0, null);

[ProtoContract(SkipConstructor = true)]
public sealed record ProtoMeshFromFacetGroup(
    [property: ProtoIgnore] RvmFacetGroup FacetGroup,
    uint TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox
) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox);

[ProtoContract(SkipConstructor = true)]
public sealed record ProtoMeshFromRvmPyramid(
    [property: ProtoIgnore] RvmPyramid Pyramid,
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
