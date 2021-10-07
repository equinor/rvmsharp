namespace CadRevealComposer.Operations.Converters
{
    using Primitives;
    using RvmSharp.Primitives;
    using System.Numerics;
    using Utils;

    public static class RvmPyramidConverter
    {
        public static APrimitive ConvertToRevealPrimitive(
            this RvmPyramid rvmPyramid,
            CommonPrimitiveProperties commonProps)
        {
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

            return new ProtoMeshFromPyramid(commonProps, rvmPyramid);
        }

        /// <summary>
        /// Q: What is "Pyramid" that has an equal Top plane size to its bottom plane, and has no offset...
        /// A: It is a box.
        /// </summary>
        private static bool IsBoxShaped(RvmPyramid rvmPyramid)
        {
            const double tolerance = 0.001f; // Arbitrary picked value

            // If it has no height, it cannot "Taper", and can be rendered as a box (or Plane, but we do not have a plane primitive).
            // FIXME: GUSH - we must ensure here 0 offset of offset less than half of top/bottom plate as some other weird shapes are possible
            if (rvmPyramid.Height.ApproximatelyEquals(0))
                return false;

            return rvmPyramid.BottomX.ApproximatelyEquals(rvmPyramid.TopX, tolerance)
                   && rvmPyramid.TopY.ApproximatelyEquals(rvmPyramid.BottomY, tolerance)
                   && rvmPyramid.OffsetX.ApproximatelyEquals(rvmPyramid.OffsetY, tolerance)
                   && rvmPyramid.OffsetX.ApproximatelyEquals(0, tolerance);
        }
    }
}