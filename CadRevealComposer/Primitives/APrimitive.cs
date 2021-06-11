// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives
{
    using Converters;
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Utils;

    public record CommonPrimitiveProperties(
        ulong NodeId,
        ulong TreeIndex,
        Vector3 Position,
        Quaternion Rotation,
        Vector3 Scale,
        float AxisAlignedDiagonal,
        int[] Color,
        (Vector3 Normal, float RotationAngle) RotationDecomposed);

    public abstract record APrimitive(
        [property: JsonProperty("node_id")]
        ulong NodeId,
        [property: JsonProperty("tree_index")] ulong TreeIndex,
        [property: I3df(I3dfAttribute.AttributeType.Color)]
        [property: JsonProperty("color")] int[] Color,
        [property: I3df(I3dfAttribute.AttributeType.Diagonal)]
        [property: JsonProperty("diagonal")] float Diagonal,
        [property: I3df(I3dfAttribute.AttributeType.CenterX)]
        [property: JsonProperty("center_x")] float CenterX,
        [property: I3df(I3dfAttribute.AttributeType.CenterY)]
        [property: JsonProperty("center_y")] float CenterY,
        [property: I3df(I3dfAttribute.AttributeType.CenterZ)]
        [property: JsonProperty("center_z")] float CenterZ,
        [property: JsonIgnore,
                   Obsolete("This is a hack to simplify inheritance. Use the other properties instead.", error: true)]
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
                commonPrimitiveProperties.Position.Z)
        {
        }

        public static APrimitive? FromRvmPrimitive(CadRevealNode revealNode, RvmNode container,
            RvmPrimitive rvmPrimitive)
        {
            PrimitiveCounter.pc++;
            var commonPrimitiveProperties = rvmPrimitive.GetCommonProps(container, revealNode);
            (ulong _, ulong _, Vector3 _, Quaternion _, Vector3 scale, float _,
                _,
                (Vector3 normal, float rotationAngle)) = commonPrimitiveProperties;

            switch (rvmPrimitive)
            {
                case RvmBox rvmBox:
                    {
                        return rvmBox.ConvertToRevealPrimitive(revealNode, container);
                    }
                case RvmCylinder rvmCylinder:
                    {
                        var height = rvmCylinder.Height * scale.Z;
                        // TODO: if scale is not uniform on X,Y, we should create something else
                        var radius = rvmCylinder.Radius * scale.X;
                        if (Math.Abs(scale.X - scale.Y) > 0.001)
                        {
                            //throw new Exception("Not implemented!");
                        }

                        if (rvmCylinder.Connections[0] != null || rvmCylinder.Connections[1] != null)
                        {
                            return new OpenCylinder
                            (
                                commonPrimitiveProperties,
                                CenterAxis: normal.CopyToNewArray(),
                                Height: height,
                                Radius: radius
                            );
                        }
                        else
                        {
                            return new ClosedCylinder
                            (
                                commonPrimitiveProperties,
                                CenterAxis: normal.CopyToNewArray(),
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
                                Normal: normal.CopyToNewArray(),
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                        else
                        {
                            return new OpenEllipsoidSegment(commonPrimitiveProperties,
                                Normal: normal.CopyToNewArray(),
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                    }
                case RvmFacetGroup:
                    PrimitiveCounter.mesh++;
                    return null;
                case RvmLine:
                    PrimitiveCounter.line++;
                    return null;
                case RvmPyramid:
                    PrimitiveCounter.pyramid++;
                    return null;
                case RvmCircularTorus circularTorus:
                    {
                        AssertUniformScale(scale);
                        var tubeRadius = circularTorus.Radius * scale.X;
                        var radius = circularTorus.Offset * scale.X;
                        if (circularTorus.Angle >= Math.PI * 2)
                        {
                            return new Torus
                            (
                                commonPrimitiveProperties,
                                Normal: normal.CopyToNewArray(),
                                Radius: radius,
                                TubeRadius: tubeRadius
                            );
                        }

                        if (circularTorus.Connections[0] != null || circularTorus.Connections[1] != null)
                            return new ClosedTorusSegment
                            (
                                commonPrimitiveProperties,
                                Normal: normal.CopyToNewArray(),
                                Radius: radius,
                                TubeRadius: tubeRadius,
                                RotationAngle: rotationAngle,
                                ArcAngle: circularTorus.Angle
                            );

                        return new OpenTorusSegment
                        (
                            commonPrimitiveProperties,
                            Normal: normal.CopyToNewArray(),
                            Radius: radius,
                            TubeRadius: tubeRadius,
                            RotationAngle: rotationAngle,
                            ArcAngle: circularTorus.Angle
                        );
                    }
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
                                Normal: normal.CopyToNewArray(),
                                Height: height,
                                Radius: radius);
                        }
                        else
                        {
                            return new ClosedSphericalSegment(commonPrimitiveProperties,
                                Normal: normal.CopyToNewArray(),
                                Height: height,
                                Radius: radius);
                        }
                    }
                case RvmSnout rvmSnout:
                    {
                        PrimitiveCounter.snout++;
                        return rvmSnout.ConvertToRevealPrimitive(revealNode, container);
                    }
                case RvmRectangularTorus rvmRectangularTorus:
                    AssertUniformScale(scale);
                    if (rvmRectangularTorus.Angle >= MathF.PI * 2)
                    {
                        return new ExtrudedRing(
                            commonPrimitiveProperties,
                            CenterAxis: normal.CopyToNewArray(),
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
                                CenterAxis: normal.CopyToNewArray(),
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
                                CenterAxis: normal.CopyToNewArray(),
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