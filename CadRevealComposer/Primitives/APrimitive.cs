// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives;

using Operations.Converters;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Utils;

// RVM model - converted to GLTF model in the end
public sealed record FacetGroup(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);
public sealed record Pyramid(Matrix4x4 Matrix, ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

// instancing processing - converted to GLTF model in the end
public abstract record ProtoMesh(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);
public sealed record ProtoMeshFromFacetGroup(FacetGroup FacetGroup, ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : ProtoMesh(TreeIndex, Color, AxisAlignedBoundingBox);
public sealed record ProtoMeshFromPyramid(Pyramid Pyramid, ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : ProtoMesh(TreeIndex, Color, AxisAlignedBoundingBox);

// Reveal GLTF model
public sealed record Box(
    Matrix4x4 InstanceMatrix,
    Color Color,
    ulong TreeIndex,
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
    Color Color, RvmBoundingBox AxisAlignedBoundingBox): APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record EccentricCone(
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 Normal,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);

public sealed record Ellipsoid(
    float HorizontalRadius,
    float VerticalRadius,
    float Height,
    Vector3 Center,
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

public sealed record Torus(
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

public sealed record InstancedMesh(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);
public sealed record TriangleMesh(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox);


public abstract record APrimitive(ulong TreeIndex, Color Color, RvmBoundingBox AxisAlignedBoundingBox)
{
    public static APrimitive? FromRvmPrimitive(
            CadRevealNode revealNode,
            RvmNode rvmNode,
            RvmPrimitive rvmPrimitive)
    {
        PrimitiveCounter.pc++;
        var commonPrimitiveProperties = rvmPrimitive.GetCommonProps(rvmNode, revealNode);
        (ulong _, ulong _, Vector3 _, Quaternion _, Vector3 scale, float _, _, _,
            (Vector3 normal, float rotationAngle), RvmPrimitive _) = commonPrimitiveProperties;

        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                return rvmBox.ConvertToRevealPrimitive(revealNode, rvmNode);
            case RvmCylinder rvmCylinder:
                {
                    var height = rvmCylinder.Height * scale.Z;
                    // TODO: if scale is not uniform on X,Y, we should create something else
                    var radius = rvmCylinder.Radius * scale.X;
                    if (!scale.X.ApproximatelyEquals(scale.Y, 0.001))
                    {
                        throw new Exception("Not implemented!");
                    }
                    return new ClosedCylinder
                    (
                        commonPrimitiveProperties,
                        CenterAxis: normal,
                        Height: height,
                        Radius: radius
                    );
                }
            case RvmEllipticalDish rvmEllipticalDish:
                {
                    AssertUniformScale(scale);
                    var verticalRadius = rvmEllipticalDish.Height * scale.X;
                    var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;
                    return new ClosedEllipsoidSegment(commonPrimitiveProperties,
                        Normal: normal,
                        Height: verticalRadius,
                        HorizontalRadius: horizontalRadius,
                        VerticalRadius: verticalRadius);
                }
            case RvmFacetGroup facetGroup:
                return new ProtoMeshFromFacetGroup(commonPrimitiveProperties, facetGroup);
            case RvmLine:
                PrimitiveCounter.line++;
                // Intentionally ignored.
                return null;
            case RvmPyramid rvmPyramid:
                return rvmPyramid.ConvertToRevealPrimitive(commonPrimitiveProperties);
            case RvmCircularTorus circularTorus:
                return circularTorus.ConvertToRevealPrimitive(rvmNode, revealNode);
            case RvmSphere rvmSphere:
                {
                    AssertUniformScale(scale);
                    var radius = rvmSphere.Radius * scale.X;
                    return new Sphere
                    (
                        commonPrimitiveProperties,
                        Radius: radius
                    );
                }
            case RvmSphericalDish rvmSphericalDish:
                {
                    AssertUniformScale(scale);
                    var height = rvmSphericalDish.Height * scale.X;
                    var baseRadius = rvmSphericalDish.BaseRadius * scale.X;
                    // radius R = h / 2 + c^2 / (8 * h), where c is the cord length or 2 * baseRadius
                    var sphereRadius = height / 2 + baseRadius * baseRadius / (2 * height);
                    var d = sphereRadius - height;
                    var upVector = Vector3.Transform(Vector3.UnitZ,
                        Matrix4x4.CreateFromQuaternion(commonPrimitiveProperties.Rotation));
                    var position = commonPrimitiveProperties.Position - upVector * d;
                    commonPrimitiveProperties = commonPrimitiveProperties with { Position = position };

                    return new ClosedSphericalSegment(commonPrimitiveProperties,
                        Normal: normal,
                        Height: height,
                        Radius: sphereRadius);
                }
            case RvmSnout rvmSnout:
                return rvmSnout.ConvertToRevealPrimitive(revealNode, rvmNode);
            case RvmRectangularTorus rvmRectangularTorus:
                AssertUniformScale(scale);
                if (rvmRectangularTorus.Angle >= MathF.PI * 2)
                {
                    return new ExtrudedRing(
                        commonPrimitiveProperties,
                        CenterAxis: normal,
                        Height: rvmRectangularTorus.Height * scale.Z,
                        InnerRadius: rvmRectangularTorus.RadiusInner * scale.X,
                        OuterRadius: rvmRectangularTorus.RadiusOuter * scale.X
                    );
                }
                else
                {
                    return new ClosedExtrudedRingSegment(
                        commonPrimitiveProperties,
                        CenterAxis: normal,
                        Height: rvmRectangularTorus.Height * scale.Z,
                        InnerRadius: rvmRectangularTorus.RadiusInner * scale.X,
                        OuterRadius: rvmRectangularTorus.RadiusOuter * scale.X,
                        RotationAngle: rotationAngle,
                        ArcAngle: rvmRectangularTorus.Angle);
                }
            default:
                throw new InvalidOperationException();
        }
    }

    private static void AssertUniformScale(Vector3 scale)
    {
        Trace.Assert(scale.IsUniform(), $"Expected uniform scale. Was {scale}.");
    }
}