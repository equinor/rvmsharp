namespace CadRevealComposer.Primitives.Instancing
{
    using Newtonsoft.Json;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Utils;

    // https://github.com/equinor/ModelOptimizationPipeline/blob/fed1215dfa26372ff0d0cb26e959cd1e8e8e85c8/tools/mop/Mop/ModelExtensions/MOPQuaternion.cs#L150

    public class RvmFacetGroupMatcher
    {
        public Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)> MatchAll(RvmFacetGroup[] groups)
        {
            return groups
                .GroupBy(CalculateKey).Select(g => (g.Key, g.ToArray())).AsParallel()
                .Select(DoMatch).SelectMany(d => d)
                .ToDictionary(r => r.Key, r => r.Value);
        }

        public Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)> DoMatch((long groupId, RvmFacetGroup[] groups) groups)
        {
            // id -> facetgroup, transform
            // templates -> facetgroup, count
            var templates = new Dictionary<RvmFacetGroup, int>();
            var result = new Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)>();

            foreach (var e in groups.groups)
            {
                bool found = false;

                foreach (var x in templates)
                {
                    if (ReferenceEquals(x.Key, e))
                        continue;
                    if (!Match(x.Key, e, out var transform))
                        continue;

                    templates[x.Key] += 1;
                    result.Add(e, (x.Key, transform.Value));
                    found = true;
                    break;
                }

                if (!found)
                {
                    templates.Add(e, 1);
                    result.Add(e, (e, Matrix4x4.Identity));
                }
            }

            Console.WriteLine($"Group completed, remaining {templates.Count} of {result.Count}");
            var i = 0;
            foreach (var t in templates)
            {
                var m = TessellatorBridge.Tessellate(t.Key, 5.0f);
                Directory.CreateDirectory($"D:\\tmp\\{groups.groupId}");
                using var objExporter = new ObjExporter($"D:\\tmp\\{groups.groupId}\\{i}.obj");
                objExporter.StartGroup(i.ToString());
                objExporter.WriteMesh(m);
                objExporter.Dispose();
                File.WriteAllText($"D:\\tmp\\{groups.groupId}\\{i}.json", JsonConvert.SerializeObject(t.Key));
                i++;
            }
            return result;
        }

        public static long CalculateKey(RvmFacetGroup facetGroup)
        {
            return facetGroup.Polygons.Length * 1000000000L
                   + facetGroup.Polygons.Sum(p => p.Contours.Length) * 1000000L
                   + facetGroup.Polygons.SelectMany(p => p.Contours).Sum(c => c.Vertices.Length);
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
            // TODO: bad assumption: polygons are ordered, contours are ordered, vertexes are ordered

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

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    for (var k = 0; k < aContour.Vertices.Length; k++)
                    {
                        var va = Vector3.Transform(aContour.Vertices[k].Vertex, transform.Value);
                        var vb = bContour.Vertices[k].Vertex;
                        if (!va.ApproximatelyEquals(vb, 0.001f))
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

            // TODO: the method below is not really correct. It is confirmed that the polygons are not sorted in  any particular order
            for (var i = 0; i < a.Polygons.Length; i++)
            {
                var aPolygon = a.Polygons[i];
                var bPolygon = b.Polygons[i];
                if (aPolygon.Contours.Length != bPolygon.Contours.Length)
                    return false;
                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];
                    if (aContour.Vertices.Length != bContour.Vertices.Length)
                        return false;
                }
            }

            var aVertices = a.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex);
            var bVertices = b.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex);

            var vertices = aVertices.Zip(bVertices);
            var testVertices = new List<(Vector3 vertexA, Vector3 vertexB)>(4);
            foreach ((Vector3 candidateVertexA, Vector3 candidateVertexB) in vertices)
            {
                if (testVertices.Any(vv => vv.vertexA.ApproximatelyEquals(candidateVertexA)))
                {
                    // skip any duplicate vertices
                    continue;
                }

                switch (testVertices.Count)
                {
                    case 3:
                        {
                            var ma = new Matrix4x4(
                                testVertices[0].vertexA.X, testVertices[1].vertexA.X, testVertices[2].vertexA.X,
                                candidateVertexA.X,
                                testVertices[0].vertexA.Y, testVertices[1].vertexA.Y, testVertices[2].vertexA.Y,
                                candidateVertexA.Y,
                                testVertices[0].vertexA.Z, testVertices[1].vertexA.Z, testVertices[2].vertexA.Z,
                                candidateVertexA.Z,
                                1, 1, 1, 1);
                            var det = ma.GetDeterminant();
                            if (!det.ApproximatelyEquals(0))
                            {
                                return TryCalculateTransform(
                                    testVertices[0].vertexA,
                                    testVertices[1].vertexA,
                                    testVertices[2].vertexA,
                                    candidateVertexA,
                                    testVertices[0].vertexB,
                                    testVertices[1].vertexB,
                                    testVertices[2].vertexB,
                                    candidateVertexB,
                                    out transform
                                );
                            }

                            break;
                        }
                    case 2:
                        {
                            var va12 = testVertices[1].vertexA - testVertices[0].vertexA;
                            var va13 = candidateVertexA - testVertices[0].vertexA;
                            if (!Vector3.Cross(va12, va13).LengthSquared().ApproximatelyEquals(0f, 0.00001))
                            {
                                testVertices.Add((candidateVertexA, candidateVertexB));
                            }

                            break;
                        }
                    default:
                        testVertices.Add((candidateVertexA, candidateVertexB));
                        break;
                }
            }
            // TODO: 2d figure
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

            var squaredBLengths = new Vector3(vb12.LengthSquared(), vb13.LengthSquared(), vb14.LengthSquared());
            var squaredALengths = new Vector3(va12.LengthSquared(), va13.LengthSquared(), va14.LengthSquared());
            var dist = (squaredALengths - squaredBLengths).Length();
            var scale = Vector3.One;
            if (!dist.ApproximatelyEquals(0))
            {
                var vaMatrix = new Matrix4x4(
                    va12.X * va12.X,va12.Y * va12.Y,va12.Z * va12.Z, 0,
                    va13.X * va13.X,va13.Y * va13.Y,va13.Z * va13.Z, 0,
                    va14.X * va14.X,va14.Y * va14.Y,va14.Z * va14.Z, 0,
                    0, 0, 0, 1);
                if (!Matrix4x4.Invert(vaMatrix, out var vaMatrixInverse))
                {
                    transform = default;
                    return false;
                }

                var scaleSquared = Vector3.Transform(squaredBLengths, Matrix4x4.Transpose(vaMatrixInverse));
                scale = new Vector3(MathF.Sqrt(scaleSquared.X), MathF.Sqrt(scaleSquared.Y), MathF.Sqrt(scaleSquared.Z));
                va12 = va12 * scale;
                va13 = va13 * scale;
            }

            // 2 rotation va'1,va'2 -> vb1,vb2
            var vaNormal = Vector3.Normalize(Vector3.Cross(va12, va13));
            var vbNormal = Vector3.Normalize(Vector3.Cross(vb12, vb13));
            var rot1 = vaNormal.FromToRotation(vbNormal);

            // 3 axis rotation: axis=vb2-vb1 va'3-va'1
            var va12r1 = Vector3.Transform(va12, rot1);
            var angle2 = va12r1.AngleTo(vb12);

            var va12r1vb12cross = Vector3.Cross(va12r1, vb12);
            var rotationNormal = Vector3.Normalize(Vector3.Cross(va12r1, vb12));
            var rot2 = va12r1vb12cross.LengthSquared().ApproximatelyEquals(0) ? Quaternion.Identity :
                Quaternion.CreateFromAxisAngle(rotationNormal, angle2);

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
    }
}
