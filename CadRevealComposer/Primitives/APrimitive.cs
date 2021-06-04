namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Utils;

    public abstract class APrimitive
    {
        [JsonProperty("node_id")] public ulong NodeId { get; set; }

        [JsonProperty("tree_index")] public ulong TreeIndex { get; set; }

        public static APrimitive? FromRvmPrimitive(CadRevealNode revealNode, RvmNode container,
            RvmPrimitive rvmPrimitive)
        {
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
                        return new Box
                        {
                            NodeId = revealNode.NodeId,
                            TreeIndex = revealNode.TreeIndex,
                            Color = colors,
                            Diagonal = axisAlignedDiagonal,
                            CenterX = pos.X,
                            CenterY = pos.Y,
                            CenterZ = pos.Z,
                            DeltaX = unitBoxScale.X,
                            DeltaY = unitBoxScale.Y,
                            DeltaZ = unitBoxScale.Z,
                            Normal = new[] {normal.X, normal.Y, normal.Z},
                            RotationAngle = rotationAngle,
                        };
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
                            {
                                NodeId = revealNode.NodeId,
                                TreeIndex = revealNode.TreeIndex,
                                Color = colors,
                                Diagonal = axisAlignedDiagonal,
                                CenterX = pos.X,
                                CenterY = pos.Y,
                                CenterZ = pos.Z,
                                CenterAxis = new[] {normal.X, normal.Y, normal.Z},
                                Height = height,
                                Radius = radius
                            };
                        }
                        else
                        {
                            return new ClosedCylinder
                            {
                                NodeId = revealNode.NodeId,
                                TreeIndex = revealNode.TreeIndex,
                                Color = colors,
                                Diagonal = axisAlignedDiagonal,
                                CenterX = pos.X,
                                CenterY = pos.Y,
                                CenterZ = pos.Z,
                                CenterAxis = new[] {normal.X, normal.Y, normal.Z},
                                Height = height,
                                Radius = radius
                            };
                        }
                    }
                case RvmCircularTorus circularTorus:
                    {
                        AssertUniformScale(scale);
                        var tubeRadius = circularTorus.Radius * scale.X;
                        var radius = circularTorus.Offset * scale.X;
                        if (circularTorus.Angle >= Math.PI * 2)
                        {
                            return new Torus
                            {
                                NodeId = revealNode.NodeId,
                                TreeIndex = revealNode.TreeIndex,
                                Color = colors,
                                Diagonal = axisAlignedDiagonal,
                                CenterX = pos.X,
                                CenterY = pos.Y,
                                CenterZ = pos.Z,
                                Normal = new[] {normal.X, normal.Y, normal.Z},
                                Radius = radius,
                                TubeRadius = tubeRadius,
                            };
                        }

                        if (circularTorus.Connections[0] != null || circularTorus.Connections[1] != null)
                            return new ClosedTorusSegment()
                            {
                                NodeId = revealNode.NodeId,
                                TreeIndex = revealNode.TreeIndex,
                                Color = colors,
                                Diagonal = axisAlignedDiagonal,
                                CenterX = pos.X,
                                CenterY = pos.Y,
                                CenterZ = pos.Z,
                                Normal = new[] {normal.X, normal.Y, normal.Z},
                                Radius = radius,
                                TubeRadius = tubeRadius,
                                RotationAngle = rotationAngle,
                                ArcAngle = circularTorus.Angle
                            };

                        return new OpenTorusSegment
                        {
                            NodeId = revealNode.NodeId,
                            TreeIndex = revealNode.TreeIndex,
                            Color = colors,
                            Diagonal = axisAlignedDiagonal,
                            CenterX = pos.X,
                            CenterY = pos.Y,
                            CenterZ = pos.Z,
                            Normal = new[] {normal.X, normal.Y, normal.Z},
                            Radius = radius,
                            TubeRadius = tubeRadius,
                            RotationAngle = rotationAngle,
                            ArcAngle = circularTorus.Angle
                        };
                    }
                case RvmSphere rvmSphere:
                    {
                        AssertUniformScale(scale);
                        var radius = (rvmSphere.Diameter / 2) * scale.X;
                        return new Sphere
                        {
                            NodeId = revealNode.NodeId,
                            TreeIndex = revealNode.TreeIndex,
                            Color = colors,
                            Diagonal = axisAlignedDiagonal,
                            CenterX = pos.X,
                            CenterY = pos.Y,
                            CenterZ = pos.Z,
                            Radius = radius 
                        };
                    }
                case RvmSnout rvmSnout:
                    {
                        AssertUniformScale(scale);
                        var height = rvmSnout.Height * scale.Z;
                        var radiusA = rvmSnout.RadiusBottom * scale.X;
                        var radiusB = rvmSnout.RadiusTop * scale.X;
                        if (rvmSnout.OffsetX == rvmSnout.OffsetY && rvmSnout.OffsetX == 0)
                        {
                            if (rvmSnout.Connections[0] != null || rvmSnout.Connections[1] != null)
                            {
                                return new OpenCone
                                {
                                    NodeId = revealNode.NodeId,
                                    TreeIndex = revealNode.TreeIndex,
                                    Color = colors,
                                    Diagonal = axisAlignedDiagonal,
                                    CenterX = pos.X,
                                    CenterY = pos.Y,
                                    CenterZ = pos.Z,
                                    CenterAxis = new[] {normal.X, normal.Y, normal.Z},
                                    Height = height,
                                    RadiusA = radiusA,
                                    RadiusB = radiusB
                                };
                            }
                            else
                            {
                                return new ClosedCone()
                                {
                                    NodeId = revealNode.NodeId,
                                    TreeIndex = revealNode.TreeIndex,
                                    Color = colors,
                                    Diagonal = axisAlignedDiagonal,
                                    CenterX = pos.X,
                                    CenterY = pos.Y,
                                    CenterZ = pos.Z,
                                    CenterAxis = new[] {normal.X, normal.Y, normal.Z},
                                    Height = height,
                                    RadiusA = radiusA,
                                    RadiusB = radiusB
                                };
                            }
                        }

                        // TODO
                        return null;
                    }
                case RvmRectangularTorus rvmRectangularTorus:
                    AssertUniformScale(scale);
                    if (rvmRectangularTorus.Angle >= MathF.PI * 2)
                    {
                        return new Ring
                        {
                            NodeId = revealNode.NodeId,
                            TreeIndex = revealNode.TreeIndex,
                            Color = colors,
                            Diagonal = axisAlignedDiagonal,
                            CenterX = pos.X,
                            CenterY = pos.Y,
                            CenterZ = pos.Z,
                            InnerRadius = rvmRectangularTorus.RadiusInner * scale.X,
                            OuterRadius = rvmRectangularTorus.RadiusOuter * scale.X
                        };
                    }
                    else
                    {
                        // TODO: segment
                        return null;
                    }
                default:
                    return null;
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