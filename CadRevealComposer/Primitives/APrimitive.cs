namespace CadRevealComposer.Primitives;

using Operations.Converters;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

// instancing processing - converted to GLTF model in the end (InstancedMesh/TriangleMesh)
public abstract record ProtoMesh(
    RvmPrimitive RvmPrimitive,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record ProtoMeshFromFacetGroup(
    RvmFacetGroup FacetGroup,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : ProtoMesh(FacetGroup, TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record ProtoMeshFromPyramid(
    RvmPyramid Pyramid,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : ProtoMesh(Pyramid, TreeIndex, Color, AxisAlignedBoundingBox);

// Reveal GLTF model
public sealed record Box(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Circle(
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Cone(
    float Angle,
    float ArcAngle,
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 LocalXAxis,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record EccentricCone(
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 Normal,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record EllipsoidSegment(
    float HorizontalRadius,
    float VerticalRadius,
    float Height,
    Vector3 Center,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record GeneralCylinder(
    float Angle,
    float ArcAngle,
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 LocalXAxis,
    Vector4 PlaneA,
    Vector4 PlaneB,
    float Radius,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record GeneralRing(
    float Angle,
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    float Thickness,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Nut(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Quad(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record TorusSegment(
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    float Radius,
    float TubeRadius,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Trapezium(
    Vector3 Vertex1,
    Vector3 Vertex2,
    Vector3 Vertex3,
    Vector3 Vertex4,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record InstancedMesh(
    int InstanceId,
    Mesh Mesh,
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record TriangleMesh(
    Mesh Mesh,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public abstract record APrimitive(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox)
{
    public static IEnumerable<APrimitive> FromRvmPrimitive(
            CadRevealNode revealNode,
            RvmNode rvmNode,
            RvmPrimitive rvmPrimitive)
    {
        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                return rvmBox.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmCylinder rvmCylinder:
                return rvmCylinder.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmEllipticalDish rvmEllipticalDish:
                return rvmEllipticalDish.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmFacetGroup rvmFacetGroup:
                return rvmFacetGroup.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmLine:
                // Intentionally ignored. Can't draw a 2D line in Cognite Reveal.
                return Array.Empty<APrimitive>();
            case RvmPyramid rvmPyramid:
                return rvmPyramid.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmCircularTorus circularTorus:
                return circularTorus.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmSphere rvmSphere:
                return rvmSphere.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmSphericalDish rvmSphericalDish:
                return rvmSphericalDish.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmSnout rvmSnout:
                return rvmSnout.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            case RvmRectangularTorus rvmRectangularTorus:
                return rvmRectangularTorus.ConvertToRevealPrimitive(revealNode.TreeIndex, rvmNode.GetColor());
            default:
                throw new ArgumentOutOfRangeException(nameof(rvmPrimitive), rvmPrimitive, nameof(rvmPrimitive));
        }
    }
}