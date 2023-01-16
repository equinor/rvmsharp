// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives;

using Newtonsoft.Json;
using Operations.Converters;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Utils;

public record CommonPrimitiveProperties(
    ulong NodeId,
    ulong TreeIndex,
    Vector3 Position,
    Quaternion Rotation,
    Vector3 Scale,
    float AxisAlignedDiagonal,
    RvmBoundingBox AxisAlignedBoundingBox,
    Color Color,
    (Vector3 Normal, float RotationAngle) RotationDecomposed,
    RvmPrimitive SourcePrimitive);

public abstract record APrimitive(
    [property: I3df(I3dfAttribute.AttributeType.Null)]
    ulong NodeId,
    [property: I3df(I3dfAttribute.AttributeType.Null)]
    ulong TreeIndex,
    [property: I3df(I3dfAttribute.AttributeType.Color)]
    Color Color,
    [property: I3df(I3dfAttribute.AttributeType.Diagonal)]
    float Diagonal,
    [property: I3df(I3dfAttribute.AttributeType.CenterX)]
    float CenterX,
    [property: I3df(I3dfAttribute.AttributeType.CenterY)]
    float CenterY,
    [property: I3df(I3dfAttribute.AttributeType.CenterZ)]
    float CenterZ,
    [property: JsonIgnore, I3df(I3dfAttribute.AttributeType.Ignore)]
    RvmBoundingBox AxisAlignedBoundingBox,
    [property: JsonIgnore, I3df(I3dfAttribute.AttributeType.Ignore)]
    RvmPrimitive SourcePrimitive,
    [property: JsonIgnore,
               Obsolete("This is a hack to simplify inheritance. Use the other properties instead.", error: true),
               I3df(I3dfAttribute.AttributeType.Ignore)]
    CommonPrimitiveProperties? CommonPrimitiveProperties =
        null! // The hack: Add JsonIgnore here, but in all inheritors use the simplified constructor.
)
{
    /// <summary>
    /// Alternative constructor to simplify inheritance
    /// </summary>
    /// <param name="commonPrimitiveProperties"></param>
    protected APrimitive(CommonPrimitiveProperties commonPrimitiveProperties)
        : this(
            commonPrimitiveProperties.NodeId,
            commonPrimitiveProperties.TreeIndex,
            commonPrimitiveProperties.Color,
            commonPrimitiveProperties.AxisAlignedDiagonal,
            commonPrimitiveProperties.Position.X,
            commonPrimitiveProperties.Position.Y,
            commonPrimitiveProperties.Position.Z,
            commonPrimitiveProperties.AxisAlignedBoundingBox,
            commonPrimitiveProperties.SourcePrimitive)
    {
    }

    // TODO: this code must be refactored, it does not belong in this namespace
    public static APrimitive? FromRvmPrimitive(
        CadRevealNode revealNode,
        RvmPrimitive rvmPrimitive)
    {
        var rvmNode = revealNode.Group as RvmNode;
        if (rvmNode == null)
        {
            Console.WriteLine($"The RvmGroup for Node {revealNode.NodeId} was null. Returning null.");
            return null;
        }

        if (rvmPrimitive.GetType() == typeof(RvmLine))
        {
            PrimitiveCounter.line++;
            // 2023-01-16 NIH: Intentionally ignored as Reveal do not support Lines, we could workaround by using a cylinder, but it may get messy
            // As far as I can see these are not visible in Navisworks either, so it may be OK to ignore
            return null;
        }

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

                    /*
                     * One case of non-uniform XY-scale on a cylinder on JSB (JS P2) was throwing an exception. Since this was the only case,
                     * it was assumed that this was an error in incoming data.
                     *
                     * To fix this specific case the largest from X and Y is chosen as the scale. Other cases with non-uniform scales should still throw an exception.
                     *
                     * https://dev.azure.com/EquinorASA/DT%20%E2%80%93%20Digital%20Twin/_workitems/edit/72816/
                     */

                    var radius = rvmCylinder.Radius * MathF.Max(scale.X, scale.Y);

                    if (scale.X != 0 && scale.Y == 0)
                    {
                        Console.WriteLine("Warning: Found cylinder where X scale was non-zero and Y scale was zero");
                    }
                    else if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
                    {
                        throw new Exception("Cylinders with non-uniform scale is not implemented!");
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