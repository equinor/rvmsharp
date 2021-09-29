namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System;
    using System.Drawing;
    using System.Numerics;
    using Utils;

    public static class RvmPrimitiveExtensions
    {
        private static Color GetColor(RvmNode container)
        {
            try
            {
                return PdmsColors.GetColorByCode(container.MaterialId);
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Fallback color is arbitrarily chosen. It seems we have some issue with the material mapping table, and should have had more colors.
                return Color.Magenta;
            }
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
            if (container.Attributes.ContainsKey("Discipline"))
            {

                switch (container.Attributes["Discipline"])
                {
                    case "ARCH":
                        colors = Color.FromArgb(85, 85, 85);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "ELEC":
                        colors = Color.FromArgb(0, 142, 142);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "HVAC":
                        colors = Color.FromArgb(149, 76, 67);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "INST":
                        colors = Color.FromArgb(133, 0, 133);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "MECH":
                        colors = Color.FromArgb(0, 122, 0);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "PIPE":
                        colors = Color.FromArgb(192, 192, 192);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "PSUP":
                        colors = Color.FromArgb(114, 114, 114);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "SAFE":
                        colors = Color.FromArgb(122, 0, 0);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "STRU":
                        colors = Color.FromArgb(182, 129, 76);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                    case "TELE":
                        colors = Color.FromArgb(122, 26, 26);
                        Console.WriteLine("Overwrite color to " + colors);
                        break;
                }
            }



            return new CommonPrimitiveProperties(
                cadNode.NodeId,
                cadNode.TreeIndex,
                pos,
                rot,
                scale,
                axisAlignedDiagonal,
                colors,
                RotationDecomposed: rot.DecomposeQuaternion());
        }
    }
}