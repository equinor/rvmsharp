// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives
{
    using Converters;
    using Newtonsoft.Json;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
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
        [property: I3df(I3dfAttribute.AttributeType.Null)]
        ulong NodeId,
        [property: I3df(I3dfAttribute.AttributeType.Null)]
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

        public static APrimitive? FromRvmPrimitive(CadRevealNode revealNode, RvmNode rvmNode,
            RvmPrimitive rvmPrimitive)
        {
            PrimitiveCounter.pc++;
            var commonPrimitiveProperties = rvmPrimitive.GetCommonProps(rvmNode, revealNode);
            (ulong _, ulong _, Vector3 _, Quaternion _, Vector3 scale, float _,
                _,
                (Vector3 normal, float rotationAngle)) = commonPrimitiveProperties;

            const uint tempHackMeshFileId = 0; // TODO: Unhardcode

            switch (rvmPrimitive)
            {
                case RvmBox rvmBox:
                    {
                        return rvmBox.ConvertToRevealPrimitive(revealNode, rvmNode);
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
                case RvmFacetGroup facetGroup:
                    const float minBoundsToExport = -1; // Temp: Adjust to create simpler models
                    if (commonPrimitiveProperties.AxisAlignedDiagonal < minBoundsToExport)
                    {
                        return null;
                    }

                    const float hardcodedTolerance = 0.1f; // TODO: Unhardcode
                    var facetGroupMesh = TessellatorBridge.Tessellate(facetGroup, tolerance: hardcodedTolerance);
                    if (facetGroupMesh == null)
                        throw new Exception($"Expected a {nameof(RvmFacetGroup)} to always tessellate. Was {facetGroupMesh}.");
                    return new TriangleMesh(
                        commonPrimitiveProperties, tempHackMeshFileId, (uint)facetGroupMesh.Triangles.Length / 3, facetGroupMesh);
                case RvmLine:
                    PrimitiveCounter.line++;
                    return null;
                case RvmPyramid rvmPyramid:
                    // A "Pyramid" that has an equal Top plane size to its bottom plane, and has no offset... is a box.
                    if (Math.Abs(rvmPyramid.BottomX - rvmPyramid.TopX) < 0.01 && Math.Abs(rvmPyramid.TopY - rvmPyramid.BottomY) < 0.01 &&
                        Math.Abs(rvmPyramid.OffsetX - rvmPyramid.OffsetY) < 0.01 && rvmPyramid.OffsetX == 0)
                    {
                        var unitBoxScale = Vector3.Multiply(
                            scale,
                            new Vector3(rvmPyramid.BottomX, rvmPyramid.BottomY, rvmPyramid.Height));
                        PrimitiveCounter.pyramidAsBox++;
                        return new Box(commonPrimitiveProperties,
                            normal.CopyToNewArray(), unitBoxScale.X,
                            unitBoxScale.Y, unitBoxScale.Z, rotationAngle);
                    }
                    else
                    {
                        // TODO: Pyramids are a good candidate for instancing. Investigate how to best apply it.
                        var pyramidMesh = TessellatorBridge.Tessellate(rvmPyramid, tolerance: -1 /* Unused for pyramids */ );

                        PrimitiveCounter.pyramid++;
                        if (pyramidMesh == null)
                            throw new Exception($"Expected a pyramid to always tessellate. Was {pyramidMesh}");

                        return new TriangleMesh(
                            commonPrimitiveProperties, tempHackMeshFileId, (uint)pyramidMesh.Triangles.Length / 3, pyramidMesh);
                    }
                case RvmCircularTorus circularTorus:
                    {
                        return circularTorus.ConvertToRevealPrimitive(rvmNode, revealNode);
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
                        return rvmSnout.ConvertToRevealPrimitive(revealNode, rvmNode);
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