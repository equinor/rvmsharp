namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System.Numerics;
    using Utils;

    public static class RvmBoxExtensions
    {
        public static Box ConvertToRevealPrimitive(this RvmBox rvmBox, CadRevealNode revealNode, RvmNode container)
        {
            var commons = rvmBox.GetCommonProps(container);
            var unitBoxScale = Vector3.Multiply(
                commons.Scale,
                new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

            return new Box(
                NodeId: revealNode.NodeId,
                TreeIndex: revealNode.TreeIndex,
                Color: commons.Color,
                Diagonal: commons.AxisAlignedDiagonal,
                Normal: commons.RotationDecomposed.Normal.CopyToNewArray(),
                CenterX: commons.Position.X,
                CenterY: commons.Position.Y,
                CenterZ: commons.Position.Z,
                DeltaX: unitBoxScale.X,
                DeltaY: unitBoxScale.Y,
                DeltaZ: unitBoxScale.Z,
                RotationAngle: commons.RotationDecomposed.RotationAngle);
        }
    }
}