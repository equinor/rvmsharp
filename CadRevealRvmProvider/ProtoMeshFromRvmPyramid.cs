namespace CadRevealRvmProvider;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;

public sealed record ProtoMeshFromRvmPyramid(
    RvmPyramid Pyramid,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox) : ProtoMesh(Pyramid, TreeIndex, Color, AxisAlignedBoundingBox);