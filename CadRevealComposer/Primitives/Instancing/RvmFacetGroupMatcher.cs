namespace CadRevealComposer.Primitives.Instancing
{
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Utils;

    // https://github.com/equinor/ModelOptimizationPipeline/blob/fed1215dfa26372ff0d0cb26e959cd1e8e8e85c8/tools/mop/Mop/ModelExtensions/MOPQuaternion.cs#L150

    public class RvmFacetGroupMatcher
    {
        private readonly ImmutableArray<RvmFacetGroup> _allFacetGroups;
        private readonly ConcurrentDictionary<RvmFacetGroup, Mesh> _previousMatches = new();

        /// <summary>
        /// Use static Create()
        /// </summary>
        private RvmFacetGroupMatcher(ImmutableArray<RvmFacetGroup> allFacetGroups)
        {
            _allFacetGroups = allFacetGroups;
        }

        public bool Match(RvmFacetGroup a, [NotNullWhen(true)] out Mesh? instancedMesh, [NotNullWhen(true)]  out Matrix4x4? transform)
        {
            foreach (var b in _allFacetGroups)
            {
                if (ReferenceEquals(a, b))
                {
                    continue;
                }

                if (Match(a, b, out var ta))
                {
                    if (!_previousMatches.TryGetValue(b, out var mesh))
                    {
                        mesh = TessellatorBridge.Tessellate(b, tolerance: 5f);
                        if (!_previousMatches.TryAdd(b, mesh)) throw new Exception("aaaa");
                    }

                    instancedMesh = mesh;
                    transform = ta;
                    return true;
                }
            }

            instancedMesh = default;
            transform = default;
            return false;
        }

        public static RvmFacetGroupMatcher Create(CadRevealNode[] cadRevealNodes)
        {
            var allFacetGroups = cadRevealNodes
                .SelectMany(x => x.RvmGeometries.OfType<RvmFacetGroup>())
                .ToImmutableArray();
            return new RvmFacetGroupMatcher(allFacetGroups);
        }

        /// <summary>
        /// Matches a to b and returns true if meshes are alike and sets transform so that a * transform = b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="outputTransform"></param>
        /// <returns></returns>
        public static bool Match(RvmFacetGroup a, RvmFacetGroup b, [NotNullWhen(true)] out Matrix4x4? outputTransform)
        {
            // probably a bad assumption: polygons are ordered, contours are ordered, vertexes are ordered

            // TODO: order polygons by contour count, order subtractive contours by vertex count
            // TODO: contour count and vertex count are not unique, must be handled properly

            if (a.Polygons.Length != b.Polygons.Length)
            {
                outputTransform = default;
                return false;
            }

            // create transform matrix
            if (!TryGetTransform(a, b, out var transform))
            {
                outputTransform = default;
                return false;
            }

            // check all polygons with transform
            for (var i = 0; i < a.Polygons.Length; i++)
            {
                var aPolygon = a.Polygons[i];
                var bPolygon = b.Polygons[i];

                if (aPolygon.Contours.Length != bPolygon.Contours.Length)
                {
                    outputTransform = default;
                    return false;
                }

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    if (aContour.Vertices.Length != bContour.Vertices.Length)
                    {
                        outputTransform = default;
                        return false;
                    }

                    for (var k = 0; k < aContour.Vertices.Length; k++)
                    {
                        if (!MatchVertexApproximately(aContour.Vertices[k].Vertex, bContour.Vertices[k].Vertex, transform.Value))
                        {
                            outputTransform = transform.Value;
                            return false;
                        }
                    }
                }
            }

            outputTransform = transform;
            return true;
        }

        public static bool TryGetTransform(RvmFacetGroup a, RvmFacetGroup b, [NotNullWhen(true)] out Matrix4x4? transform)
        {
            transform = default;
            if (a.Polygons.Length != b.Polygons.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Polygons.Length; i++)
            {
                var aPolygon = a.Polygons[i];
                var bPolygon = b.Polygons[i];

                if (aPolygon.Contours.Length != bPolygon.Contours.Length)
                {
                    return false;
                }

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    if (aContour.Vertices.Length != bContour.Vertices.Length)
                    {
                        return false;
                    }
                }
            }

            using var aVertices = a.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex).GetEnumerator();
            using var bVertices = b.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex).GetEnumerator();

            var testVertices = new List<(Vector3, Vector3)>(4);
            while (aVertices.MoveNext() && bVertices.MoveNext())
            {
                var ca = aVertices.Current;
                var cb = bVertices.Current;

                var alreadyHave = testVertices.Any(vv => vv.Item1 == ca);

                if (!alreadyHave)
                {
                    if (testVertices.Count == 3)
                    {
                        var va12 = testVertices[1].Item1 - testVertices[0].Item1;
                        var va13 = testVertices[2].Item1 - testVertices[0].Item1;
                        var va14 = ca - testVertices[0].Item1;
                        if (!Determinant(va12, va13, va14).ApproximatelyEquals(0.001f))
                        {
                            return TryCalculateTransform(
                                testVertices[0].Item1,
                                testVertices[1].Item1,
                                testVertices[2].Item1,
                                ca,
                                testVertices[0].Item2,
                                testVertices[1].Item2,
                                testVertices[2].Item2,
                                cb,
                                out transform
                            );

                        }
                    }
                    else
                    {
                        if (testVertices.Count == 2)
                        {
                            var va12 = testVertices[1].Item1 - testVertices[0].Item1;
                            var va13 = ca - testVertices[0].Item1;
                            if (!Vector3.Cross(va12, va13).LengthSquared().ApproximatelyEquals(0f, 0.00001))
                            {
                                testVertices.Add((ca, cb));
                            }
                        }
                        else
                        {
                            testVertices.Add((ca, cb));
                        }

                    }
                }
            }

            transform = default;
            return false;
        }

        public static bool TryCalculateTransform(Vector3 pa1, Vector3 pa2, Vector3 pa3, Vector3 pa4, Vector3 pb1, Vector3 pb2, Vector3 pb3, Vector3 pb4, [NotNullWhen(true)] out Matrix4x4? transform)
        {
            var va12 = pa2 - pa1;
            var va13 = pa3 - pa1;
            var va14 = pa4 - pa1;
            var vb12 = pb2 - pb1;
            var vb13 = pb3 - pb1;
            var vb14 = pb4 - pb1;

            var vaMatrix = new Matrix4x4(
                va12.X * va12.X,va12.Y * va12.Y,va12.Z * va12.Z, 0,
                va13.X * va13.X,va13.Y * va13.Y,va13.Z * va13.Z, 0,
                va14.X * va14.X,va14.Y * va14.Y,va14.Z * va14.Z, 0,
                0, 0, 0, 1);

            var squaredBLengths = new Vector3(vb12.Length() * vb12.Length(), vb13.Length() * vb13.Length(), vb14.Length() * vb14.Length());
            if (!Matrix4x4.Invert(vaMatrix, out var vaMatrixInverse))
            {
                transform = default;
                return false;
                throw new Exception("Could not invert matrix for scale");
            }
            var scale = new Vector3(
                MathF.Sqrt(vaMatrixInverse.M11 * squaredBLengths.X + vaMatrixInverse.M12 * squaredBLengths.Y + vaMatrixInverse.M13 * squaredBLengths.Z),
                MathF.Sqrt(vaMatrixInverse.M21 * squaredBLengths.X + vaMatrixInverse.M22 * squaredBLengths.Y + vaMatrixInverse.M23 * squaredBLengths.Z),
                MathF.Sqrt(vaMatrixInverse.M31 * squaredBLengths.X + vaMatrixInverse.M32 * squaredBLengths.Y + vaMatrixInverse.M33 * squaredBLengths.Z));

            va12 = va12 * scale;
            va13 = va13 * scale;

            // 2 rotation va'1,va'2 -> vb1,vb2
            var vaNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(va12), Vector3.Normalize(va13)));
            var vbNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(vb12), Vector3.Normalize(vb13)));
            var rot1 = vaNormal.FromToRotation(vbNormal);

            // 3 axis rotation: axis=vb2-vb1 va'3-va'1
            var va12r1 = Vector3.Transform(va12, rot1);
            var angle2 = va12r1.AngleTo(vb12);
            var rotationNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(va12r1), Vector3.Normalize(vb12)));
            var rot2 = Quaternion.CreateFromAxisAngle(rotationNormal, angle2);

            var rotation = rot2 * rot1;

            // translation
            var translation = pb1 - Vector3.Transform(pa1 * scale, rotation);

            transform =
                Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(translation);
            return true;
        }

        /// <summary>
        /// Match a * transform = b
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MatchVertexApproximately(Vector3 a, Vector3 b, Matrix4x4 transform)
        {
            return Vector3.Transform(a, transform).ApproximatelyEquals(b, tolerance: 0.00001f); // TODO: ok tolerance?
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Determinant(Vector3 a, Vector3 b, Vector3 c)
        {
            return
                a.X * b.Y * c.Z +
                b.X * c.Y * a.Z +
                c.X * a.Y * b.Z -
                c.X * b.Y * a.Z -
                b.X * a.Y * c.Z -
                a.X * c.Y * b.Z;
        }
    }
}
