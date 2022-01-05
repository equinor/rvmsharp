namespace CadRevealComposer.Operations.Converters
{
    using Primitives;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Utils;

    public static class RvmSnoutConverter
    {
        private static (float slope, float zangle) TranslateShearToSlope((float shearX, float shearY) input)
        {
            var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -input.shearX);
            var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, input.shearY);
            var rotationTotal = rotationAroundX * rotationAroundY;
            var capNormal = Vector3.Transform(Vector3.UnitZ, rotationTotal);
            var angleAroundZ = MathF.Atan2(capNormal.Y, capNormal.X);
            var slope = MathF.PI / 2 - MathF.Atan2(capNormal.Z, MathF.Sqrt(capNormal.X * capNormal.X + capNormal.Y * capNormal.Y));

            return (slope, angleAroundZ);
        }

        public static APrimitive ConvertToRevealPrimitive(this RvmSnout rvmSnout, CadRevealNode revealNode,
            RvmNode container)
        {
            var commons = rvmSnout.GetCommonProps(container, revealNode);
            var scale = commons.Scale;
            Trace.Assert(scale.IsUniform(), $"Expected Uniform scale, was {scale}");
            var height = MathF.Sqrt(
                rvmSnout.Height * rvmSnout.Height + rvmSnout.OffsetX * rvmSnout.OffsetX + rvmSnout.OffsetY * rvmSnout.OffsetY) * scale.Z;

            var radiusA = rvmSnout.RadiusTop * scale.X;
            var radiusB = rvmSnout.RadiusBottom * scale.X;

            if (HasShear(rvmSnout))
            {
                if (IsEccentric(rvmSnout))
                {
                    throw new NotImplementedException(
                        "This type of primitive is missing from CadReveal, should convert to mesh?");
                }
                else
                {
                    (float slopeA, float zangleA) = TranslateShearToSlope((rvmSnout.TopShearX, rvmSnout.TopShearY));
                    (float slopeB, float zangleB) = TranslateShearToSlope((rvmSnout.BottomShearX, rvmSnout.BottomShearY));
                    if (rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom))
                    {
                        // General cylinder
                        return new ClosedGeneralCylinder(
                            commons,
                            CenterAxis: commons.RotationDecomposed.Normal,
                            Height: height,
                            Radius: radiusA,
                            RotationAngle: commons.RotationDecomposed.RotationAngle,
                            ArcAngle: 2 * MathF.PI,
                            SlopeA: slopeA,
                            SlopeB: slopeB,
                            ZangleA: zangleA + commons.RotationDecomposed.RotationAngle,
                            ZangleB: zangleB + commons.RotationDecomposed.RotationAngle
                        );
                    } else {
                        // General cone
                        return new ClosedGeneralCone(
                            commons,
                            CenterAxis: commons.RotationDecomposed.Normal,
                            Height: height,
                            RadiusA: radiusA,
                            RadiusB: radiusB,
                            RotationAngle: commons.RotationDecomposed.RotationAngle,
                            ArcAngle: 2 * MathF.PI,
                            SlopeA: slopeA,
                            SlopeB: slopeB,
                            ZangleA: zangleA + commons.RotationDecomposed.RotationAngle,
                            ZangleB: zangleB + commons.RotationDecomposed.RotationAngle
                        );
                    }
                }
            }
            else
            {
                if (IsEccentric(rvmSnout))
                {
                    var capNormal = Vector3.Transform(Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X), commons.Rotation);
                    return new ClosedEccentricCone(commons,
                        CenterAxis: capNormal,
                        Height: height,
                        RadiusA: radiusA,
                        RadiusB: radiusB,
                        CapNormal: commons.RotationDecomposed.Normal);
                }
                else
                {
                    return new ClosedCone(
                        commons,
                        CenterAxis: commons.RotationDecomposed.Normal,
                        Height: height,
                        RadiusA: radiusA,
                        RadiusB: radiusB);
                }
            }
        }

        private static bool IsEccentric(RvmSnout rvmSnout)
        {
            return rvmSnout.OffsetX != 0 || rvmSnout.OffsetY != 0;
        }

        private static bool HasShear(RvmSnout rvmSnout)
        {
            return rvmSnout.BottomShearX != 0 || rvmSnout.BottomShearY != 0 || rvmSnout.TopShearX != 0 ||
                   rvmSnout.TopShearY != 0;
        }
    }
}