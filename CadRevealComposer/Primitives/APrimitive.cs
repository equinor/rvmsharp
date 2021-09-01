// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives
{
    using Converters;
    using Instancing;
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
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

        public static APrimitive? FromRvmPrimitive(
            CadRevealNode revealNode,
            RvmNode rvmNode,
            RvmPrimitive rvmPrimitive,
            RvmFacetGroupMatcher rvmFacetGroupMatcher)
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

                    if (rvmFacetGroupMatcher.Match(facetGroup, out var instancedMesh, out var transform))
                    {
                        if (!Matrix4x4.Decompose(transform.Value, out var s, out var r, out var t))
                        {
                            throw new InvalidOperationException();
                        }
                        // TODO: rotation Euler?
                        return new InstancedMesh(commonPrimitiveProperties, 0, 0, 0, t.X, t.Y, t.Z, r.X, r.Y, r.Z, s.X, s.Y, s.Z)
                        {
                            Mesh = instancedMesh
                        };
                    }

                    const float minBoundsToExport = -1; // Temp: Adjust to create simpler models
                    if (commonPrimitiveProperties.AxisAlignedDiagonal < minBoundsToExport)
                    {
                        return null;
                    }

                    const float unusedTolerance = 5f; // Tolerance is currently unused for FacetGroups
                    var facetGroupMesh = TessellatorBridge.Tessellate(facetGroup, tolerance: unusedTolerance);
                    if (facetGroupMesh == null)
                        throw new Exception(
                            $"Expected a {nameof(RvmFacetGroup)} to always tessellate. Was {facetGroupMesh}.");
                    return new TriangleMesh(
                        commonPrimitiveProperties, tempHackMeshFileId, (uint)facetGroupMesh.Triangles.Count / 3,
                        facetGroupMesh);
                case RvmLine:
                    PrimitiveCounter.line++;
                    return null;
                case RvmPyramid rvmPyramid:
                    return rvmPyramid.ConvertToRevealPrimitive(tempHackMeshFileId, revealNode, rvmNode);
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
                    {
                        return rvmSnout.ConvertToRevealPrimitive(revealNode, rvmNode);
                    }
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