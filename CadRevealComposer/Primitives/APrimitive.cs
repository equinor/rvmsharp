// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives
{
    using Converters;
    using Newtonsoft.Json;
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
        (Vector3 Normal, float RotationAngle) RotationDecomposed);

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
                commonPrimitiveProperties.AxisAlignedBoundingBox)
        {
        }

        // TODO: this code must be refactored, it does not belong in this namespace
        public static APrimitive? FromRvmPrimitive(
            CadRevealNode revealNode,
            RvmNode rvmNode,
            RvmPrimitive rvmPrimitive,
            PyramidInstancingHelper? pyramidInstancingHelper = null)
        {
            PrimitiveCounter.pc++;
            var commonPrimitiveProperties = rvmPrimitive.GetCommonProps(rvmNode, revealNode);
            (ulong _, ulong _, Vector3 _, Quaternion _, Vector3 scale, float _, _, _,
                (Vector3 normal, float rotationAngle)) = commonPrimitiveProperties;

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
                            //throw new Exception("Not implemented!");
                        }

                        if (rvmCylinder.Connections[0] != null || rvmCylinder.Connections[1] != null)
                        {
                            return new OpenCylinder
                            (
                                commonPrimitiveProperties,
                                CenterAxis: normal,
                                Height: height,
                                Radius: radius
                            );
                        }
                        else
                        {
                            return new ClosedCylinder
                            (
                                commonPrimitiveProperties,
                                CenterAxis: normal,
                                Height: height,
                                Radius: radius
                            );
                        }
                    }
                case RvmEllipticalDish rvmEllipticalDish:
                    {
                        AssertUniformScale(scale);
                        var verticalRadius = rvmEllipticalDish.Height * scale.X;
                        var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;
                        if (rvmEllipticalDish.Connections[0] != null)
                        {
                            return new ClosedEllipsoidSegment(commonPrimitiveProperties,
                                Normal: normal,
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                        else
                        {
                            return new OpenEllipsoidSegment(commonPrimitiveProperties,
                                Normal: normal,
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                    }
                case RvmFacetGroup facetGroup:
                    return new ProtoMesh(commonPrimitiveProperties, facetGroup);
                case RvmLine:
                    PrimitiveCounter.line++;
                    // Intentionally ignored.
                    return null;
                case RvmPyramid rvmPyramid:
                    return rvmPyramid.ConvertToRevealPrimitive(revealNode, rvmNode, pyramidInstancingHelper);
                case RvmCircularTorus circularTorus:
                    return circularTorus.ConvertToRevealPrimitive(rvmNode, revealNode);
                case RvmSphere rvmSphere:
                    {
                        AssertUniformScale(scale);
                        var radius = (rvmSphere.Diameter / 2) * scale.X;
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
                        var radius = rvmSphericalDish.BaseRadius * scale.X;
                        if (rvmSphericalDish.Connections[0] != null)
                        {
                            return new ClosedSphericalSegment(commonPrimitiveProperties,
                                Normal: normal,
                                Height: height,
                                Radius: radius);
                        }
                        else
                        {
                            return new ClosedSphericalSegment(commonPrimitiveProperties,
                                Normal: normal,
                                Height: height,
                                Radius: radius);
                        }
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
                        if (rvmRectangularTorus.Connections[0] != null && rvmRectangularTorus.Connections[1] != null)
                        {
                            return new OpenExtrudedRingSegment(
                                commonPrimitiveProperties,
                                CenterAxis: normal,
                                Height: rvmRectangularTorus.Height * scale.Z,
                                InnerRadius: rvmRectangularTorus.RadiusInner * scale.X,
                                OuterRadius: rvmRectangularTorus.RadiusOuter * scale.X,
                                RotationAngle: rotationAngle,
                                ArcAngle: rvmRectangularTorus.Angle);
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
}