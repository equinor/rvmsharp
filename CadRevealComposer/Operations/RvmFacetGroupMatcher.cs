namespace CadRevealComposer.Operations
{
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Utils;

    public static class RvmFacetGroupMatcher
    {
        private static RvmFacetGroup BakeTransformAndCenter(RvmFacetGroup input, bool centerMesh, out Matrix4x4 translationMatrix)
        {
            var originalMatrix = input.Matrix;

            // Calculate bounds for new bounding box
            var minBounds = new Vector3(float.MaxValue);
            var maxBounds = new Vector3(float.MinValue);
            foreach (var v in input.Polygons
                .SelectMany(p => p.Contours)
                .SelectMany(c => c.Vertices)
                .Select(vn => vn.Vertex))
            {
                var vt = Vector3.Transform(v, originalMatrix);
                minBounds = Vector3.Min(vt, minBounds);
                maxBounds = Vector3.Max(vt, maxBounds);
            }

            var extents = (maxBounds - minBounds) / 2;

            var finalMatrix = originalMatrix;
            translationMatrix = Matrix4x4.Identity;
            if (centerMesh)
            {
                var groupCenter = minBounds + extents;
                var centerOffsetMatrix = Matrix4x4.CreateTranslation(-groupCenter);
                translationMatrix = Matrix4x4.CreateTranslation(groupCenter);

                minBounds = -extents;
                maxBounds = extents;

                finalMatrix = originalMatrix * centerOffsetMatrix;
            }

            // Transforming mesh normals requires some extra calculations.
            // https://web.archive.org/web/20210628111622/https://paroj.github.io/gltut/Illumination/Tut09%20Normal%20Transformation.html
            if (!Matrix4x4.Invert(finalMatrix, out var matrixInverted))
                throw new ArgumentException($"Could not invert matrix {finalMatrix}");
            var matrixInvertedTransposed = Matrix4x4.Transpose(matrixInverted);

            var polygons = input.Polygons.Select(p => p with
            {
                Contours = p.Contours
                    .Select(c => new RvmFacetGroup.RvmContour(c.Vertices.Select(vn =>
                        (Vector3.Transform(vn.Vertex, finalMatrix), Vector3.TransformNormal(vn.Normal, matrixInvertedTransposed))
                        ).ToArray())).ToArray()
            }).ToArray();

            return input with
            {
                Polygons = polygons,
                BoundingBoxLocal = new RvmBoundingBox(minBounds, maxBounds),
                Matrix = Matrix4x4.Identity
            };
        }


        public static Dictionary<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)> MatchAll(RvmFacetGroup[] groups)
        {

            var groupedGroups = groups.AsParallel().GroupBy(CalculateKey).Select(g => (g.Key, facetGroups: g.ToArray())).ToArray();

            var result = groupedGroups.AsParallel().Select(
                    g => MatchGroups2(g.facetGroups)).SelectMany(d => d)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return result;
        }

        private static Dictionary<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)> MatchGroups2(
            RvmFacetGroup[] inputFacetGroups)
        {
            var result = new Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)>();
            var templates = new List<RvmFacetGroup>();
            foreach (var facetGroup in inputFacetGroups)
            {
                var matchFound = false;
                var bakedFacetGroup = BakeTransformAndCenter(facetGroup, false, out _);
                foreach (var templateCandidate in templates)
                {
                    if (!Match(templateCandidate, bakedFacetGroup, out var transform))
                        continue;
                    matchFound = true;
                    result.Add(facetGroup, (templateCandidate, transform));
                    break;
                }

                if (matchFound)
                    continue;

                var newTemplate = BakeTransformAndCenter(facetGroup, true, out var templateToFacetGroupTransform);
                templates.Add(newTemplate);
                result.Add(facetGroup, (newTemplate, templateToFacetGroupTransform));
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

        public static bool VerifyTransform(RvmFacetGroup a, RvmFacetGroup b, Matrix4x4 transform)
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
            if (!EnsurePolygonContoursAndVertexCountsMatch(a, b))
            {
                transform = default;
                return false;
            }

            var aVertices = a.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex);
            var bVertices = b.Polygons.SelectMany(p => p.Contours).SelectMany(c => c.Vertices).Select((vn) => vn.Vertex);

            var vertices = aVertices.Zip(bVertices);
            var testVertices = new List<(Vector3 vertexA, Vector3 vertexB)>(4);
            var maxDet = 0f;
            foreach ((Vector3 candidateVertexA, Vector3 candidateVertexB) in vertices)
            {
                if (testVertices.Any(vv => vv.vertexA.ApproximatelyEquals(candidateVertexA, 0.005f)))
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
                            maxDet = MathF.Max(MathF.Abs(det), maxDet);
                            if (!det.ApproximatelyEquals(0, 0.00000001f))
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
                            var va12 = Vector3.Normalize(testVertices[1].vertexA - testVertices[0].vertexA);
                            var va13 = Vector3.Normalize(candidateVertexA - testVertices[0].vertexA);
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
            transform = default;
            return false;
        }

        private static bool EnsurePolygonContoursAndVertexCountsMatch(RvmFacetGroup a, RvmFacetGroup b)
        {
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
