namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Numerics;
    using Utils;

    public static class RvmPyramidConverter
    {
        public static APrimitive ConvertToRevealPrimitive(this RvmPyramid rvmPyramid,
            ulong meshFileId,
            CadRevealNode revealNode,
            RvmNode container)
        {
            var commonProps = rvmPyramid.GetCommonProps(container, revealNode);

            if (IsBoxShaped(rvmPyramid))
            {
                var unitBoxScale = Vector3.Multiply(
                    commonProps.Scale,
                    new Vector3(rvmPyramid.BottomX, rvmPyramid.BottomY, rvmPyramid.Height));
                PrimitiveCounter.pyramidAsBox++;
                return new Box(commonProps,
                    commonProps.RotationDecomposed.Normal, unitBoxScale.X,
                    unitBoxScale.Y, unitBoxScale.Z, commonProps.RotationDecomposed.RotationAngle);
            }
            else
            {
                // TODO: Pyramids are a good candidate for instancing. Investigate how to best apply it.
                var pyramidMesh = TessellatorBridge.Tessellate(rvmPyramid, tolerance: -1 /* Unused for pyramids */);

                PrimitiveCounter.pyramid++;
                if (pyramidMesh == null)
                    throw new Exception($"Expected a pyramid to always tessellate. Was {pyramidMesh}");

                return new TriangleMesh(
                    commonProps, meshFileId, (uint)pyramidMesh.Triangles.Length / 3, pyramidMesh);
            }
        }

        /// <summary>
        /// Q: What is "Pyramid" that has an equal Top plane size to its bottom plane, and has no offset...
        /// A: It is a box.
        /// </summary>
        private static bool IsBoxShaped(RvmPyramid rvmPyramid)
        {
            const double tolerance = 0.01; // Arbitrary picked value

            return rvmPyramid.BottomX.NearlyEquals(rvmPyramid.TopX, tolerance)
                   && rvmPyramid.TopY.NearlyEquals(rvmPyramid.BottomY, tolerance)
                   && rvmPyramid.OffsetX.NearlyEquals(rvmPyramid.OffsetY, tolerance)
                   && rvmPyramid.OffsetX.NearlyEquals(0, tolerance);
        }
    }
}