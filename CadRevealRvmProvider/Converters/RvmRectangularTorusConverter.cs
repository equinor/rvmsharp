namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using Commons.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmRectangularTorusConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(this RvmRectangularTorus rvmRectangularTorus,
        ulong treeIndex,
        Color color, string area)
    {
        if (!rvmRectangularTorus.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmRectangularTorus.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        if (rvmRectangularTorus.RadiusOuter <= 0 || rvmRectangularTorus.RadiusInner < 0)
        {
            Console.WriteLine(
                $"Removing RectangularTorus because radius was invalid. Outer radius: {rvmRectangularTorus.RadiusOuter} Inner radius: {rvmRectangularTorus.RadiusInner}"
            );
            yield break;
        }

        (Vector3 normal, float _) = rotation.DecomposeQuaternion();

        var radiusInner = rvmRectangularTorus.RadiusInner * scale.X;
        var radiusOuter = rvmRectangularTorus.RadiusOuter * scale.X;

        if (radiusOuter <= 0)
        {
            Console.WriteLine($"Rectangular Torus was removed, because outer radius was: {radiusOuter}");
            yield break;
        }

        var thickness = (radiusOuter - radiusInner) / radiusOuter;

        var outerDiameter = radiusOuter * 2;
        var height = rvmRectangularTorus.Height * scale.Y;
        var halfHeight = height / 2.0f;

        var centerA = position + normal * halfHeight;
        var centerB = position - normal * halfHeight;

        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);
        var arcAngle = rvmRectangularTorus.Angle;

        var bbBox = rvmRectangularTorus.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        if (height != 0)
        {
            yield return new Cone(
                0,
                arcAngle,
                centerA,
                centerB,
                localToWorldXAxis,
                radiusOuter,
                radiusOuter,
                treeIndex,
                color,
                bbBox, area
            );

            // If inner radius equals 0, then the geometry is basically a cylinder segment, and the inner cone is unnecessary
            if (radiusInner > 0)
            {
                yield return new Cone(
                    0,
                    arcAngle,
                    centerA,
                    centerB,
                    localToWorldXAxis,
                    radiusInner,
                    radiusInner,
                    treeIndex,
                    color,
                    bbBox, area
                );
            }
        }

        var matrixRingA =
            Matrix4x4.CreateScale(outerDiameter)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        if (matrixRingA.IsDecomposable())
        {
            yield return new GeneralRing(0f, arcAngle, matrixRingA, normal, thickness, treeIndex, color, bbBox, area);
        }
        else
        {
            // This should not happen, but happens in so few models as of now that we think we can ignore it.
            Console.WriteLine(
                $"Failed to decompose matrix for {nameof(matrixRingA)} of node {treeIndex} geometry: {rvmRectangularTorus}"
            );
        }

        if (height != 0)
        {
            var matrixRingB =
                Matrix4x4.CreateScale(outerDiameter)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

            if (matrixRingB.IsDecomposable())
            {
                yield return new GeneralRing(0f, arcAngle, matrixRingB, -normal, thickness, treeIndex, color, bbBox, area);
            }
            else
            {
                // This should not happen, but happens in so few models as of now that we think we can ignore it.
                Console.WriteLine(
                    $"Failed to decompose matrix for {nameof(matrixRingB)} of node {treeIndex} geometry: {rvmRectangularTorus}"
                );
            }

            // Add caps to the two ends of the torus, where the segment is "cut out"
            // This is not needed if the torus goes all the way around
            var isTorusSegment = !arcAngle.ApproximatelyEquals(2 * MathF.PI);
            if (isTorusSegment)
            {
                var v1 = localToWorldXAxis;

                var q2 = Quaternion.CreateFromAxisAngle(normal, arcAngle);
                var v2 = Vector3.Transform(v1, q2);

                var centerQuadA = (centerA + centerB + v1 * (radiusInner + radiusOuter)) / 2.0f;
                var centerQuadB = (centerA + centerB + v2 * (radiusInner + radiusOuter)) / 2.0f;

                var halfPiAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
                var halfPiAroundZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
                var arcRotationCompensation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, arcAngle);
                var rotationQuadA = rotation * halfPiAroundX * halfPiAroundZ;
                var rotationQuadB = rotation * arcRotationCompensation * halfPiAroundX * halfPiAroundZ;

                var scaleQuad = new Vector3(height, radiusOuter - radiusInner, 0);

                var quadMatrixA =
                    Matrix4x4.CreateScale(scaleQuad)
                    * Matrix4x4.CreateFromQuaternion(rotationQuadA)
                    * Matrix4x4.CreateTranslation(centerQuadA);

                var quadMatrixB =
                    Matrix4x4.CreateScale(scaleQuad)
                    * Matrix4x4.CreateFromQuaternion(rotationQuadB)
                    * Matrix4x4.CreateTranslation(centerQuadB);

                yield return new Quad(quadMatrixA, treeIndex, color, bbBox, area);

                yield return new Quad(quadMatrixB, treeIndex, color, bbBox, area);
            }
        }
    }
}
