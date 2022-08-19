namespace CadRevealRvmProvider;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using System.Drawing;
using RvmSharp.Primitives;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
public abstract record ProtoMesh(
    RvmPrimitive RvmPrimitive,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record ProtoMeshFromFacetGroup(
    RvmFacetGroup FacetGroup,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox);
