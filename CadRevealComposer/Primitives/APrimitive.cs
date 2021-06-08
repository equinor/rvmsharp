// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Linq;
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
            if (!Matrix4x4.Decompose(rvmPrimitive.Matrix, out var scale, out var rot, out var pos))
            {
                throw new Exception("Failed to decompose matrix." + rvmPrimitive.Matrix);
            }

            var axisAlignedDiagonal = rvmPrimitive.CalculateAxisAlignedBoundingBox().Diagonal;

            var colors = GetColor(container);
            (Vector3 normal, float rotationAngle) = rot.DecomposeQuaternion();

            switch (rvmPrimitive)
            {
                case RvmBox rvmBox:
                    {
                        var unitBoxScale = Vector3.Multiply(scale,
                            new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

                        return new Box(
                            NodeId: revealNode.NodeId,
                            TreeIndex: revealNode.TreeIndex,
                            Color: colors,
                            Diagonal: axisAlignedDiagonal,
                            Normal: normal.CopyToNewArray(),
                            CenterX: pos.X,
                            CenterY: pos.Y,
                            CenterZ: pos.Z,
                            DeltaX: unitBoxScale.X,
                            DeltaY: unitBoxScale.Y,
                            DeltaZ: unitBoxScale.Z,
                            RotationAngle: rotationAngle);
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
                    PrimitiveCounter.eDish++;
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
                        var height = rvmSphericalDish.Height;
                        var radius = rvmSphericalDish.BaseRadius;
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
                        AssertUniformScale(scale);
                        var height = rvmSnout.Height * scale.Z;
                        var radiusB = rvmSnout.RadiusBottom * scale.X;
                        var radiusA = rvmSnout.RadiusTop * scale.X;
                        if (rvmSnout.OffsetX == rvmSnout.OffsetY && rvmSnout.OffsetX == 0)
                        {
                            if (rvmSnout.Connections[0] != null || rvmSnout.Connections[1] != null)
                            {
                                return new OpenCone
                                    (NodeId: revealNode.NodeId,
                                        TreeIndex: revealNode.TreeIndex,
                                        Color: colors,
                                        Diagonal: axisAlignedDiagonal,
                                        CenterX: pos.X,
                                        CenterY: pos.Y,
                                        CenterZ: pos.Z,
                                        CenterAxis: new[] {normal.X, normal.Y, normal.Z},
                                        Height: height,
                                        RadiusA: radiusA,
                                        RadiusB: radiusB)
                                    ;
                            }
                            else
                            {
                                return new ClosedCone(
                                    NodeId: revealNode.NodeId,
                                    TreeIndex: revealNode.TreeIndex,
                                    Color: colors,
                                    Diagonal: axisAlignedDiagonal,
                                    CenterX: pos.X,
                                    CenterY: pos.Y,
                                    CenterZ: pos.Z,
                                    CenterAxis: new[] {normal.X, normal.Y, normal.Z},
                                    Height: height,
                                    RadiusA: radiusA,
                                    RadiusB: radiusB);
                                ;
                            }
                        }
                        PrimitiveCounter.snout++;
                        // TODO
                        return null;
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

        private static int[] GetColor(RvmNode container)
        {
            // TODO: Fallback color is arbitrarily chosen, it should probably be handled differently
            return PdmsColors.GetColorAsBytesByCode(container.MaterialId < 50 ? container.MaterialId : 1)
                .Select(x => (int)x).ToArray();
        }
    }
}