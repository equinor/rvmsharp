namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System.Numerics;

    public static class RvmBoxExtensions
    {
        public static Box ConvertToRevealPrimitive(this RvmBox rvmBox, CadRevealNode revealNode, RvmNode container)
        {
            var commons = rvmBox.GetCommonProps(container, revealNode);
            var unitBoxScale = Vector3.Multiply(
                commons.Scale,
                new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

            Box revealBox = new Box(
                CommonPrimitiveProperties: commons,
                Normal: commons.RotationDecomposed.Normal,
                DeltaX: unitBoxScale.X,
                DeltaY: unitBoxScale.Y,
                DeltaZ: unitBoxScale.Z,
                RotationAngle: commons.RotationDecomposed.RotationAngle);

            return revealBox;
        }
    }
}