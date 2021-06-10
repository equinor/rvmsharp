namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Utils;

    public static class RvmSnoutConverter
    {
        public static APrimitive? ConvertToRevealPrimitive(this RvmSnout rvmSnout, CadRevealNode revealNode,
            RvmNode container)
        {
            var commons = rvmSnout.GetCommonProps(container,revealNode);
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
                    // TODO: general cylinder if radius A == radius B
                    /*if (IsOpen(rvmSnout))
                    {
                        return OpenGeneralCone();
                    }
                    else
                    {
                        return ClosedGeneralCone();
                    }*/
                }
            } else 
            {
                if (IsEccentric(rvmSnout))
                {
                    var capNormal = Vector3.Transform(Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X), commons.Rotation);
                    if (IsOpen(rvmSnout))
                    {
                        return new OpenEccentricCone(
                            commons,
                            CenterAxis: capNormal.CopyToNewArray(),
                            Height: height,
                            RadiusA: radiusA,
                            RadiusB: radiusB,
                            CapNormal: commons.RotationDecomposed.Normal.CopyToNewArray());
                    }
                    else
                    {
                        return new ClosedEccentricCone(commons,
                            CenterAxis: capNormal.CopyToNewArray(),
                            Height: height,
                            RadiusA: radiusA,
                            RadiusB: radiusB,
                            CapNormal: commons.RotationDecomposed.Normal.CopyToNewArray());
                    }
                } else {
                    if (IsOpen(rvmSnout))
                    {
                        var c = new OpenCone(
                            commons,
                            CenterAxis: commons.RotationDecomposed.Normal.CopyToNewArray(),
                            Height: height,
                            RadiusA: radiusA,
                            RadiusB: radiusB);

                        return c;
                    }
                    else
                    {
                        return new ClosedCone(
                            commons,
                            CenterAxis: commons.RotationDecomposed.Normal.CopyToNewArray(),
                            Height: height,
                            RadiusA: radiusA,
                            RadiusB: radiusB);
                    }
                }
            }
            return null;
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

        private static bool IsOpen(RvmSnout rvmSnout)
        {
            return rvmSnout.Connections[0] != null || rvmSnout.Connections[1] != null;
        }
    }
}