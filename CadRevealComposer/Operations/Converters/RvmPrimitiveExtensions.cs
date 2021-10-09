namespace CadRevealComposer.Operations.Converters
{
    using Primitives;
    using RvmSharp.Primitives;
    using System;
    using System.Drawing;
    using System.Numerics;
    using Utils;

    public static class RvmPrimitiveExtensions
    {
        private static Color GetColor(RvmNode container)
        {
            if (PdmsColors.TryGetColorByCode(container.MaterialId, out var color))
            {
                return color;
            }

            // TODO: Fallback color is arbitrarily chosen. It seems we have some issue with the material mapping table, and should have had more colors.
            return Color.Magenta;
        }

        /// <summary>
        /// Retrieve the common properties, that are present for all RvmPrimitives.
        /// Converted to world space.
        /// </summary>
        internal static CommonPrimitiveProperties GetCommonProps(this RvmPrimitive rvmPrimitive, RvmNode container, CadRevealNode cadNode)
        {
            if (!Matrix4x4.Decompose(rvmPrimitive.Matrix, out var scale, out var rot, out var pos))
            {
                throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmPrimitive.Matrix);
            }
            rot = Quaternion.Normalize(rot);

            var axisAlignedBoundingBox = rvmPrimitive.CalculateAxisAlignedBoundingBox();

            var colors = GetColor(container);

            return new CommonPrimitiveProperties(
                cadNode.NodeId,
                cadNode.TreeIndex,
                pos,
                rot,
                scale,
                axisAlignedBoundingBox.Diagonal,
                axisAlignedBoundingBox,
                colors,
                RotationDecomposed: rot.DecomposeQuaternion());
        }
    }
}