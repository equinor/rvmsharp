namespace CadRevealComposer.Operations
{
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Utils;

    public static class RvmFacetGroupMatcher
    {
        /// <summary>
        /// Mutable to allow fast sorting of templates by swapping properties.
        /// </summary>
        private class TemplateItem
        {
            public TemplateItem(RvmFacetGroup template)
            {
                Template = template;
            }

            public RvmFacetGroup Template { get; set; }
            public int MatchCount { get; set; }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static RvmFacetGroup BakeTransformAndCenter(RvmFacetGroup facetGroup, bool centerMesh, out Matrix4x4 translationMatrix)
        {
            var originalMatrix = facetGroup.Matrix;

            // Calculate bounds for new bounding box
            var minBounds = new Vector3(float.MaxValue);
            var maxBounds = new Vector3(float.MinValue);
            foreach (var v in facetGroup.Polygons
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

            var polygons = facetGroup.Polygons.Select(p => p with
            {
                Contours = p.Contours
                    .Select(c => new RvmFacetGroup.RvmContour(
                        c.Vertices
                        .Select(vn => (Vector3.Transform(vn.Vertex, finalMatrix), Vector3.TransformNormal(vn.Normal, matrixInvertedTransposed)))
                        .ToArray()))
                    .ToArray()
            }).ToArray();

            return facetGroup with
            {
                Polygons = polygons,
                BoundingBoxLocal = new RvmBoundingBox(minBounds, maxBounds),
                Matrix = Matrix4x4.Identity
            };
        }

        public static Dictionary<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)> MatchAll(RvmFacetGroup[] facetGroups, uint instancingThreshold)
        {
            var groupingTimer = Stopwatch.StartNew();
            var groupedFacetGroups =
                facetGroups
                    .AsParallel()
                    .GroupBy(CalculateKey)
                    .Where(x => x.Count() >= instancingThreshold) // We can ignore all groups of less items than the threshold.
                    .Select(g => (g.Key, FacetGroups: g.OrderByDescending(x => x.BoundingBoxLocal.Diagonal).ToArray()))
                    .ToArray();

            var facetGroupForMatchingCount = groupedFacetGroups.Sum(x => x.FacetGroups.Length);
            Console.WriteLine($"Found {groupedFacetGroups.Length} groups with more than {instancingThreshold} items for a count of {facetGroupForMatchingCount} facet groups of total {facetGroups.Length} in {groupingTimer.Elapsed}");
            Console.WriteLine("Algorithm is O(n^2) of group size (worst case).");
            var matchingTimer = Stopwatch.StartNew();
            var result =
                groupedFacetGroups
                    .OrderByDescending(x => x.FacetGroups.Length)
                    .AsParallel()
                    .Select(g => MatchGroups(g.FacetGroups))
                    .SelectMany(d => d)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var uniqueTemplateCount = result.DistinctBy(x => x.Value.template).Count();
            var fraction = 1.0 - (uniqueTemplateCount / (float)facetGroupForMatchingCount);
            Console.WriteLine($"Found {uniqueTemplateCount} unique from a total of {facetGroupForMatchingCount} ({fraction:P1}). Time: {matchingTimer.Elapsed}");
            return result;
        }

        private static Dictionary<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)> MatchGroups(RvmFacetGroup[] facetGroups)
        {
            static void SwapItemData(TemplateItem a, TemplateItem b)
            {
                var aTemplate = a.Template;
                var aMatchCount = a.MatchCount;
                a.Template = b.Template;
                a.MatchCount = b.MatchCount;
                b.Template = aTemplate;
                b.MatchCount = aMatchCount;
            }

            var result = new Dictionary<RvmFacetGroup, (RvmFacetGroup, Matrix4x4)>();
            var templates = new List<TemplateItem>(); // sorted high to low by explicit call

            var timer = Stopwatch.StartNew();
            var matchCount = 0;
            foreach (var facetGroup in facetGroups)
            {
                var matchFoundFromPreviousTemplates = false;
                var bakedFacetGroup = BakeTransformAndCenter(facetGroup, false, out _);

                for (var i = 0; i < templates.Count; i++)
                {
                    var item = templates[i];
                    matchCount++;
                    if (!Match(item.Template, bakedFacetGroup, out var transform))
                    {
                        continue;
                    }

                    result.Add(facetGroup, (item.Template, transform));
                    item.MatchCount++;

                    // sort template list descending by match count
                    var templateMatchCount = item.MatchCount;
                    var j = i;
                    while (j - 1 >= 0 && templateMatchCount > templates[j - 1].MatchCount)
                    {
                        j--;
                    }
                    if (j != i) // swap items
                    {
                        SwapItemData(item, templates[j]);
                    }

                    matchFoundFromPreviousTemplates = true;
                    break;
                }

                if (matchFoundFromPreviousTemplates)
                {
                    continue;
                }

                var newTemplate = BakeTransformAndCenter(facetGroup, true, out var newTransform);
                templates.Add(new TemplateItem(newTemplate));
                result.Add(facetGroup, (newTemplate, newTransform));
            }

            var templateCount = result.DistinctBy(x => x.Value.Item1).Count();
            var vertexCount = facetGroups.First().Polygons.Sum(x => x.Contours.Sum(y => y.Vertices.Length));
            var fraction = 1.0 - (templateCount / (float)facetGroups.Length);
            Console.WriteLine(
                $"\tFound {templateCount,5:N0} templates in {facetGroups.Length,6:N0} items ({fraction,6:P1}). " +
                $"Vertex count was {vertexCount,5:N0} in {timer.Elapsed.TotalSeconds,6:N} s. {matchCount:N0} iterations.");
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
            unchecked
            {
                // Based on https://stackoverflow.com/a/263416
                long key = 17;
                const long hashMultiplier = 486187739;
                key = key * hashMultiplier + facetGroup.Polygons.LongLength;

                for (var i = 0; i < facetGroup.Polygons.LongLength; i++)
                {
                    var contours = facetGroup.Polygons[i].Contours;
                    key = key * hashMultiplier + contours.LongLength;
                    for (var j = 0; j < contours.LongLength; j++)
                    {
                        key = key * hashMultiplier + contours[j].Vertices.LongLength;
                    }
                }

                return key;
            }
        }

        /// <summary>
        /// Matches a to b and returns true if meshes are alike and sets transform so that a * transform = b.
        /// </summary>
        /// <param name="aFacetGroup"></param>
        /// <param name="bFacetGroup"></param>
        /// <param name="outputTransform"></param>
        /// <returns></returns>
        public static bool Match(RvmFacetGroup aFacetGroup, RvmFacetGroup bFacetGroup, out Matrix4x4 outputTransform)
        {
            if (GetPossibleAtoBTransform(aFacetGroup, bFacetGroup, out outputTransform))
            {
                return VerifyTransform(aFacetGroup, bFacetGroup, outputTransform);
            }

            outputTransform = default;
            return false;
        }

        /// <summary>
        /// For each vertex verify that a * transform = b.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static bool VerifyTransform(RvmFacetGroup aFacetGroup, RvmFacetGroup bFacetGroup, in Matrix4x4 transform)
        {
            // REMARK: array bound checks are expensive -> polygons/contours/vertices count is assumed to be equal due to grouping by CalculateKey()

            for (var i = 0; i < aFacetGroup.Polygons.Length; i++)
            {
                var aPolygon = aFacetGroup.Polygons[i];
                var bPolygon = bFacetGroup.Polygons[i];

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    for (var k = 0; k < aContour.Vertices.Length; k++)
                    {
                        var transformedVector = Vector3.Transform(aContour.Vertices[k].Vertex, transform);
                        var vb = bContour.Vertices[k].Vertex;
                        if (!transformedVector.EqualsWithinFactorOrTolerance(vb, 0.001f, 0.001f))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static bool GetPossibleAtoBTransform(RvmFacetGroup aFacetGroup, RvmFacetGroup bFacetGroup, out Matrix4x4 transform)
        {
            // REMARK: array bound checks are expensive -> polygons/contours/vertices count is assumed to be equal due to grouping by CalculateKey()

            // REMARK: it is assumed that polygons are ordered to improve matching - see RvmParser

            (Vector3 vertexA, Vector3 vertexB, bool isSet) testVertex1 = (Vector3.Zero, Vector3.Zero, false);
            (Vector3 vertexA, Vector3 vertexB, bool isSet) testVertex2 = (Vector3.Zero, Vector3.Zero, false);
            (Vector3 vertexA, Vector3 vertexB, bool isSet) testVertex3 = (Vector3.Zero, Vector3.Zero, false);

            for (var i = 0; i < aFacetGroup.Polygons.Length; i++)
            {
                var aPolygon = aFacetGroup.Polygons[i];
                var bPolygon = bFacetGroup.Polygons[i];

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    for (var k = 0; k < aContour.Vertices.Length; k++)
                    {
                        var candidateVertexA = aContour.Vertices[k].Vertex;
                        var candidateVertexB = bContour.Vertices[k].Vertex;

                        const float factor = 0.001f; // 0.1%
                        var isDuplicateVertex = testVertex1.isSet &&
                                                testVertex1.vertexA.EqualsWithinFactor(candidateVertexA, factor) ||
                                                testVertex2.isSet &&
                                                testVertex2.vertexA.EqualsWithinFactor(candidateVertexA, factor) ||
                                                testVertex3.isSet &&
                                                testVertex3.vertexA.EqualsWithinFactor(candidateVertexA, factor);
                        if (isDuplicateVertex)
                        {
                            // ignore duplicate vertex
                        }
                        else if (!testVertex1.isSet)
                        {
                            testVertex1 = (candidateVertexA, candidateVertexB, true);
                        }
                        else if (!testVertex2.isSet)
                        {
                            testVertex2 = (candidateVertexA, candidateVertexB, true);
                        }
                        else if (!testVertex3.isSet)
                        {
                            var va12 = Vector3.Normalize(testVertex2.vertexA - testVertex1.vertexA);
                            var va13 = Vector3.Normalize(candidateVertexA - testVertex1.vertexA);
                            if (!Vector3.Cross(va12, va13).LengthSquared().ApproximatelyEquals(0f))
                            {
                                testVertex3 = (candidateVertexA, candidateVertexB, true);
                            }
                        }
                        else
                        {
                            // at this point all three test vertices are set
                            var ma = new Matrix4x4(
                                testVertex1.vertexA.X, testVertex2.vertexA.X, testVertex3.vertexA.X, candidateVertexA.X,
                                testVertex1.vertexA.Y, testVertex2.vertexA.Y, testVertex3.vertexA.Y, candidateVertexA.Y,
                                testVertex1.vertexA.Z, testVertex2.vertexA.Z, testVertex3.vertexA.Z, candidateVertexA.Z,
                                1, 1, 1, 1);
                            var determinant = ma.GetDeterminant();
                            if (!determinant.ApproximatelyEquals(0, 0.000_001f))
                            {
                                return AlgebraUtils.GetTransform(
                                    testVertex1.vertexA,
                                    testVertex2.vertexA,
                                    testVertex3.vertexA,
                                    candidateVertexA,
                                    testVertex1.vertexB,
                                    testVertex2.vertexB,
                                    testVertex3.vertexB,
                                    candidateVertexB,
                                    out transform
                                );
                            }
                        }
                    }
                }
            }

            // TODO: 2d figure
            transform = default;
            return false;
        }
    }
}