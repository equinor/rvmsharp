namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System;
    using System.Linq;
    using System.Numerics;
    using Utils;

    public static class RvmPrimitiveExtensions
    {
        private static int[] GetColor(RvmNode container)
        {
            // TODO: Fallback color is arbitrarily chosen, it should probably be handled differently
            return PdmsColors.GetColorAsBytesByCode(container.MaterialId < 50 ? container.MaterialId : 1)
                .Select(x => (int)x).ToArray();
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

            var axisAlignedDiagonal = rvmPrimitive.CalculateAxisAlignedBoundingBox().Diagonal;

            var colors = GetColor(container);

            return new CommonPrimitiveProperties(cadNode.NodeId, cadNode.TreeIndex,pos, rot, scale, axisAlignedDiagonal, colors, RotationDecomposed: rot.DecomposeQuaternion());
        }
    }
}