namespace CadRevealComposer.Primitives.Instancing
{
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Utils;

    public static class RvmFacetGroupMatcher
    {
        public static Dictionary<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)> MatchAll(RvmFacetGroup[] groups)
        {
            return groups
                .GroupBy(CalculateKey).Select(g => (g.Key, g.ToArray())).AsParallel()
                .Select(DoMatch).SelectMany(d => d)
                .ToDictionary(r => r.Key, r => r.Value);
        }

        private static Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)> DoMatch((long groupId, RvmFacetGroup[] groups) groups)
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
                    result.Add(e, (x.Key, transform));
                    found = true;
                    break;
                }

                if (!found)
                {
                    templates.Add(e, 1);
                    result.Add(e, (e, Matrix4x4.Identity));
                }
            }

            return result;
        }

        /// <summary>
        /// to compose a unique key for a facet group we use polygon count in billions, total contour count in millions
        /// and vertex count added together. This will give us keys with very few collision where counts are different
        /// the key is used to create compare buckets of facet groups. There is no point to compare facet groups with
        /// different keys, since they will always be different
        /// </summary>
        /// <param name="facetGroup">facet group to calculate a key for</param>
        /// <returns>a key reflection information amount in facet group</returns>
        public static long CalculateKey(RvmFacetGroup facetGroup)
        {
            return facetGroup.Polygons.Length * 1000_000_000L
                   + facetGroup.Polygons.Sum(p => p.Contours.Length) * 1000_000L
                   + facetGroup.Polygons.SelectMany(p => p.Contours).Sum(c => c.Vertices.Length);
        }

        /// <summary>
        /// Matches a to b and returns true if meshes are alike and sets transform so that a * transform = b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="outputTransform"></param>
        /// <returns></returns>
        public static bool Match(RvmFacetGroup a, RvmFacetGroup b, out Matrix4x4 outputTransform)
        {
            // TODO: bad assumption: polygons are ordered, contours are ordered, vertexes are ordered
            // create transform matrix
            if (GetPossibleAtoBTransform(a, b, out outputTransform))
            {
                return VerifyTransform(a, b, outputTransform);
            }

            outputTransform = default;
            return false;
        }

        private static bool VerifyTransform(RvmFacetGroup a, RvmFacetGroup b, Matrix4x4 transform)
        {
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
                        var va = Vector3.Transform(aContour.Vertices[k].Vertex, transform);
                        var vb = bContour.Vertices[k].Vertex;
                        if (!va.ApproximatelyEquals(vb, 0.001f))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool GetPossibleAtoBTransform(RvmFacetGroup a, RvmFacetGroup b, out Matrix4x4 transform)
        {
            if (!EnsurePolygonContoursAndVertexCountsMatch(a, b, out transform))
            {
                return false;
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
                                return AlgebraUtils.GetTransform(
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
                            if (!Vector3.Cross(va12, va13).LengthSquared().ApproximatelyEquals(0f))
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

        private static bool EnsurePolygonContoursAndVertexCountsMatch(RvmFacetGroup a, RvmFacetGroup b, out Matrix4x4 transform)
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

            return true;
        }
    }
}
