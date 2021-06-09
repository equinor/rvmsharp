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

    public abstract record APrimitive(
        [property: JsonProperty("node_id")] ulong NodeId,
        [property: JsonProperty("tree_index")] ulong TreeIndex,
        [property: JsonProperty("color")] int[] Color,
        [property: JsonProperty("diagonal")] float Diagonal,
        [property: JsonProperty("center_x")] float CenterX,
        [property: JsonProperty("center_y")] float CenterY,
        [property: JsonProperty("center_z")] float CenterZ
    )
    {
        public static APrimitive? FromRvmPrimitive(CadRevealNode revealNode, RvmNode container,
            RvmPrimitive rvmPrimitive)
        {
            PrimitiveCounter.pc++;
            (Vector3 pos, Quaternion _, Vector3 scale, float axisAlignedDiagonal, var colors,
                (Vector3 normal, float rotationAngle)) = rvmPrimitive.GetCommonProps(container);

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
                                NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                CenterAxis: normal.CopyToNewArray(),
                                Height: height,
                                Radius: radius
                            );
                        }
                        else
                        {
                            return new ClosedCylinder
                            (
                                NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
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
                            return new ClosedEllipsoidSegment(NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                Normal: normal.CopyToNewArray(),
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                        else
                        {
                            return new OpenEllipsoidSegment(NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                Normal: normal.CopyToNewArray(),
                                Height: verticalRadius,
                                HorizontalRadius: horizontalRadius,
                                VerticalRadius: verticalRadius);
                        }
                    }
                    return null;
                case RvmFacetGroup rvmFacetGroup:
                    PrimitiveCounter.mesh++;
                    return null;
                case RvmLine rvmLine:
                    PrimitiveCounter.line++;
                    return null;
                case RvmPyramid rvmPyramid:
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
                                NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                Normal: normal.CopyToNewArray(),
                                Radius: radius,
                                TubeRadius: tubeRadius
                            );
                        }

                        if (circularTorus.Connections[0] != null || circularTorus.Connections[1] != null)
                            return new ClosedTorusSegment
                            (
                                NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                Normal: normal.CopyToNewArray(),
                                Radius: radius,
                                TubeRadius: tubeRadius,
                                RotationAngle: rotationAngle,
                                ArcAngle: circularTorus.Angle
                            );

                        return new OpenTorusSegment
                        (
                            NodeId: revealNode.NodeId,
                            TreeIndex: revealNode.TreeIndex,
                            Color: colors,
                            Diagonal: axisAlignedDiagonal,
                            CenterX: pos.X,
                            CenterY: pos.Y,
                            CenterZ: pos.Z,
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
                            NodeId: revealNode.NodeId,
                            TreeIndex: revealNode.TreeIndex,
                            Color: colors,
                            Diagonal: axisAlignedDiagonal,
                            CenterX: pos.X,
                            CenterY: pos.Y,
                            CenterZ: pos.Z,
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
                            return new ClosedSphericalSegment(NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
                                Normal: normal.CopyToNewArray(),
                                Height: height,
                                Radius: radius);
                        }
                        else
                        {
                            return new ClosedSphericalSegment(NodeId: revealNode.NodeId,
                                TreeIndex: revealNode.TreeIndex,
                                Color: colors,
                                Diagonal: axisAlignedDiagonal,
                                CenterX: pos.X,
                                CenterY: pos.Y,
                                CenterZ: pos.Z,
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
                        return new Ring(
                            NodeId: revealNode.NodeId,
                            TreeIndex: revealNode.TreeIndex,
                            Color: colors,
                            Diagonal: axisAlignedDiagonal,
                            CenterX: pos.X,
                            CenterY: pos.Y,
                            CenterZ: pos.Z,
                            InnerRadius: rvmRectangularTorus.RadiusInner * scale.X,
                            OuterRadius: rvmRectangularTorus.RadiusOuter * scale.X
                        );
                    }
                    else
                    {
                        PrimitiveCounter.rTorus++;
                        // TODO: segment
                        return null;
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