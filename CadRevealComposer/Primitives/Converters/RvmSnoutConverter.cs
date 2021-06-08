namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using Utils;

    public static class RvmConeConverter
    {
        public static APrimitive? ConvertToRevealPrimitive(this RvmSnout rvmSnout, CadRevealNode revealNode,
            RvmNode container)
        {
            var commons = rvmSnout.GetCommonProps(container);
            var scale = commons.Scale;
            Trace.Assert(scale.IsUniform(), $"Expected Uniform scale, was {scale}");
            var height = rvmSnout.Height * scale.Z;
            
            var radiusA = rvmSnout.RadiusTop * scale.X;
            var radiusB = rvmSnout.RadiusBottom * scale.X;

            if (rvmSnout.OffsetX == 0 && Math.Abs(rvmSnout.OffsetX - rvmSnout.OffsetY) < 0.001)
            {
                if (rvmSnout.Connections[0] != null || rvmSnout.Connections[1] != null)
                {
                    return new OpenCone(
                        NodeId: revealNode.NodeId,
                        TreeIndex: revealNode.TreeIndex,
                        Color: commons.Color,
                        Diagonal: commons.AxisAlignedDiagonal,
                        CenterX: commons.Position.X,
                        CenterY: commons.Position.Y,
                        CenterZ: commons.Position.Z,
                        CenterAxis: commons.RotationDecomposed.Normal.CopyToNewArray(),
                        Height: height,
                        RadiusA: radiusA,
                        RadiusB: radiusB);
                }
                else
                {
                    return new ClosedCone(
                        NodeId: revealNode.NodeId,
                        TreeIndex: revealNode.TreeIndex,
                        Color: commons.Color,
                        Diagonal: commons.AxisAlignedDiagonal,
                        CenterX: commons.Position.X,
                        CenterY: commons.Position.Y,
                        CenterZ: commons.Position.Z,
                        CenterAxis: commons.RotationDecomposed.Normal.CopyToNewArray(),
                        Height: height,
                        RadiusA: radiusA,
                        RadiusB: radiusB);
                }
            }

            // TODO: Missing implementation

            return null;
        }
    }
}