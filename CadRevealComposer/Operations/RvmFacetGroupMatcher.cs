namespace CadRevealComposer.Operations;

using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Utils;

public static class RvmFacetGroupMatcher
{
    public abstract record Result(RvmFacetGroup FacetGroup);

    public record NotInstancedResult(RvmFacetGroup FacetGroup) : Result(FacetGroup);

    public record InstancedResult
        (RvmFacetGroup FacetGroup, RvmFacetGroup Template, Matrix4x4 Transform) : Result(FacetGroup);

    public record TemplateResult
        (RvmFacetGroup FacetGroup, RvmFacetGroup Template, Matrix4x4 Transform) : InstancedResult(FacetGroup, Template,
            Transform);

    private const int TemplateCleanupInterval = 500; // Arbitrarily chosen number

    /// <summary>
    /// Mutable to allow fast sorting of templates by swapping properties.
    /// </summary>
    private class TemplateItem
    {
        public TemplateItem(RvmFacetGroup original, RvmFacetGroup template, Matrix4x4 transform)
        {
            Original = original;
            Template = template;
            Transform = transform;
        }

        public RvmFacetGroup Original { get; set; }
        public RvmFacetGroup Template { get; set; }
        public Matrix4x4 Transform { get; set; }
        public int MatchCount { get; set; }
        public int MatchAttempts { get; set; }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static RvmFacetGroup BakeTransformAndCenter(RvmFacetGroup facetGroup, bool centerMesh,
        out Matrix4x4 translationMatrix)
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
                        .Select(vn => (Vector3.Transform(vn.Vertex, finalMatrix),
                            Vector3.TransformNormal(vn.Normal, matrixInvertedTransposed)))
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

    public static Result[] MatchAll(RvmFacetGroup[] allFacetGroups, Func<RvmFacetGroup[], bool> shouldInstance)
    {
        var groupingTimer = Stopwatch.StartNew();
        var groupedFacetGroups =
            allFacetGroups
                .AsParallel()
                .GroupBy(CalculateKey)
                .Select(g => g.OrderByDescending(x => x.BoundingBoxLocal.Diagonal).ToArray())
                .ToArray();

        var groupCount = groupedFacetGroups.Count(shouldInstance);
        var facetGroupForMatchingCount = groupedFacetGroups
            .Where(shouldInstance)
            .Sum(facetGroups => facetGroups.Length);
        Console.WriteLine(
            $"Found {groupCount:N0} groups for a count of {facetGroupForMatchingCount:N0} facet groups " +
            $"of total {allFacetGroups.Length:N0} in {groupingTimer.Elapsed}");
        Console.WriteLine("Algorithm is O(n^2) of group size (worst case).");
        Console.WriteLine("Explanations. IC: iteration count, TC: template count, VC: vertex count");

        (IReadOnlyList<Result> Result, long IterationCounter) MatchGroup(RvmFacetGroup[] facetGroups)
        {
            var result = new List<Result>();

            if (shouldInstance(facetGroups) is false)
            {
                foreach (var facetGroup in facetGroups)
                {
                    result.Add(new NotInstancedResult(facetGroup));
                }

                // return early so the group isn't logged to console
                return (result, 0L);
            }

            var timer = Stopwatch.StartNew();
            var instancingResults = MatchFacetGroups(facetGroups, out var iterationCounter);

            // post determine if group is adequate for instancing
            var templateCount = 0L;
            var instancedCount = 0L;
            foreach (var instancingGroup in instancingResults.ToLookup(x => x is InstancedResult))
            {
                // is not instanced
                if (instancingGroup.Key is false)
                {
                    foreach (var instanceResult in instancingGroup)
                    {
                        result.Add(instanceResult);
                    }

                    continue;
                }

                // is instanced
                var instanceGroups = instancingGroup
                    .OfType<InstancedResult>()
                    .GroupBy(r => r.Template);

                foreach (var instanceGroup in instanceGroups)
                {
                    var fg = instanceGroup
                        .Select(x => x.FacetGroup)
                        .ToArray();
                    var shouldInstanceGroup = shouldInstance(fg);

                    foreach (var instancedResult in instanceGroup)
                    {
                        if (shouldInstanceGroup)
                        {
                            instancedCount++;
                            if (instancedResult is TemplateResult)
                            {
                                templateCount++;
                            }
                        }

                        result.Add(shouldInstanceGroup
                            ? instancedResult
                            : new NotInstancedResult(instancedResult.FacetGroup));
                    }
                }
            }

            var vertexCount = facetGroups
                .First()
                .Polygons.Sum(x => x.Contours.Sum(y => y.Vertices.Length));
            var fraction = instancedCount / (float)facetGroups.Length;
            Console.WriteLine(
                $"\tFound {instancedCount,7:N0} instances in {facetGroups.Length,7:N0} items ({fraction,6:P1})." +
                $" TC: {templateCount,3:N0}, VC: {vertexCount,5:N0}, IC: {iterationCounter:N0} in {timer.Elapsed.TotalSeconds,6:N} s.");

            return (result, iterationCounter);
        }

        long iterationCounter = 0;

        var result =
            groupedFacetGroups
                .OrderByDescending(facetGroups => facetGroups.Length)
                .AsParallel()
                .SelectMany(x =>
                {
                    var result = MatchGroup(x);
                    Interlocked.Add(ref iterationCounter, result.IterationCounter);
                    return result.Result;
                })
                .ToArray();

        var templateCount = result.OfType<TemplateResult>().Count();
        var instancedCount = result.OfType<InstancedResult>().Count();
        var fraction = instancedCount / (float)allFacetGroups.Length;
        Console.WriteLine(
            $"Facet groups found {templateCount:N0} unique representing {instancedCount:N0} instances " +
            $"from a total of {allFacetGroups.Length:N0} ({fraction:P1}).");
        Console.WriteLine($"Total iteration count: {iterationCounter}");

        if (result.Length != allFacetGroups.Length)
        {
            throw new Exception($"Input and output count doesn't match up. {allFacetGroups.Length} vs {result.Length}");
        }

        return result;
    }

    private static List<Result> MatchFacetGroups(RvmFacetGroup[] facetGroups, out long iterationCounter)
    {
        static void SwapItemData(TemplateItem a, TemplateItem b)
        {
            var aOriginal = a.Original;
            var aTemplate = a.Template;
            var aTransform = a.Transform;
            var aMatchCount = a.MatchCount;
            var aMatchAttempts = a.MatchAttempts;

            a.Original = b.Original;
            a.Template = b.Template;
            a.Transform = b.Transform;
            a.MatchCount = b.MatchCount;
            a.MatchAttempts = b.MatchAttempts;

            b.Original = aOriginal;
            b.Template = aTemplate;
            b.Transform = aTransform;
            b.MatchCount = aMatchCount;
            b.MatchAttempts = aMatchAttempts;
        }

        var result = new List<Result>();
        var templateCandidates = new List<TemplateItem>(); // sorted high to low by explicit code

        var iterCounter = 0L;
        var matchingTimer = Stopwatch.StartNew();
        var target = TimeSpan.FromMinutes(5);
        var cleanupIntervalCounter = 0;
        foreach (var facetGroup in facetGroups)
        {
            cleanupIntervalCounter++;
            if (matchingTimer.Elapsed > target)
            {
                var groupKey = CalculateKey(facetGroup);
                var vertexCount = facetGroups
                    .First()
                    .Polygons.Sum(x => x.Contours.Sum(y => y.Vertices.Length));
                Console.WriteLine($"Grouping with {vertexCount} vertices taking a long time. More than {(int)target.TotalMinutes} minutes. Group key is: {groupKey}");
                target += TimeSpan.FromMinutes(5);
            }

            var matchFoundFromPreviousTemplates = false;
            var bakedFacetGroup = BakeTransformAndCenter(facetGroup, false, out _);

            for (var i = 0; i < templateCandidates.Count; i++)
            {
                var item = templateCandidates[i];
                item.MatchAttempts++;
                iterCounter++;
                if (!Match(item.Template, bakedFacetGroup, out var transform))
                {
                    continue;
                }

                result.Add(new InstancedResult(facetGroup, item.Template, transform));
                item.MatchCount++;

                // sort template list descending by match count
                var templateMatchCount = item.MatchCount;
                var j = i;
                while (j - 1 >= 0 && templateMatchCount > templateCandidates[j - 1].MatchCount)
                {
                    j--;
                }

                if (j != i) // swap items
                {
                    SwapItemData(item, templateCandidates[j]);
                }

                matchFoundFromPreviousTemplates = true;
                break;
            }

            if (matchFoundFromPreviousTemplates)
            {
                continue;
            }

            var newTemplate = BakeTransformAndCenter(facetGroup, true, out var newTransform);

            // To avoid comparing with too many templates, making the worst case O(N^2),
            // we remove the templates with the least number of matches every once in a while
            if (cleanupIntervalCounter > TemplateCleanupInterval)
            {
                CleanupTemplateCandidates(templateCandidates, result, facetGroups.Length);
                cleanupIntervalCounter = 0;
            }

            templateCandidates.Add(new TemplateItem(facetGroup, newTemplate, newTransform));
        }

        foreach (var template in templateCandidates)
        {
            Result r = template.MatchCount > 0
                ? new TemplateResult(template.Original, template.Template, template.Transform)
                : new NotInstancedResult(template.Original);

            result.Add(r);
        }

        iterationCounter = iterCounter;
        return result;
    }

    private static void CleanupTemplateCandidates(List<TemplateItem> templateCandidates, List<Result> result,
        int facetGroupsLength)
    {
        // Give up on templates that have had X attempts, but less than Y% matches.
        var templatesToGiveUpOn = templateCandidates
            .Where(x =>
                    x.MatchAttempts > Math.Max(500, Math.Min(facetGroupsLength / 300, 3000))
                    && (double)x.MatchCount / (x.MatchAttempts) < 0.001 // If match count is low we discard it
            )
            .ToHashSet();

        if (templatesToGiveUpOn.Any())
        {
            // Console.WriteLine("Gave up on " + templatesToGiveUpOn.Count);


            result.AddRange(templatesToGiveUpOn
                .Select(x => new NotInstancedResult(x.Original)));
            templateCandidates.RemoveAll(x => templatesToGiveUpOn.Contains(x));
        }
    }

    /// <summary>
    /// Identifies a facet group with 2 parallel triangles with 3 rectangular sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecialCaseVolumeTriangle(RvmFacetGroup facetGroup)
    {
        return facetGroup.Polygons.Length == 5 &&
               facetGroup.Polygons[0].Contours.Length == 1 &&
               facetGroup.Polygons[1].Contours.Length == 1 &&
               facetGroup.Polygons[2].Contours.Length == 1 &&
               facetGroup.Polygons[3].Contours.Length == 1 &&
               facetGroup.Polygons[4].Contours.Length == 1 &&
               facetGroup.Polygons[0].Contours[0].Vertices.Length == 3 &&
               facetGroup.Polygons[1].Contours[0].Vertices.Length == 3 &&
               facetGroup.Polygons[2].Contours[0].Vertices.Length == 4 &&
               facetGroup.Polygons[3].Contours[0].Vertices.Length == 4 &&
               facetGroup.Polygons[4].Contours[0].Vertices.Length == 4;
    }

    /// <summary>
    /// Support method for use with <see cref="IsSpecialCaseVolumeTriangle"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetFirstAngleForSpecialCaseVolumeTriangleInDegrees(RvmFacetGroup facetGroup)
    {
        var triangle = facetGroup.Polygons[0].Contours[0];
        var v1 = triangle.Vertices[0].Vertex;
        var v2 = triangle.Vertices[1].Vertex;
        var v3 = triangle.Vertices[2].Vertex;

        var v12 = v1 - v2;
        var v13 = v1 - v3;

        return v12.AngleTo(v13) * 180f / MathF.PI;
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

            // The special case is for Melk�ya which has 885k of these. With O(N^2) this takes time, so let's divide this group into smaller groups.
            // Create groups for every 15 degrees using the first angle in the triangle.
            return IsSpecialCaseVolumeTriangle(facetGroup)
                ? key + (long)(MathF.Round(GetFirstAngleForSpecialCaseVolumeTriangleInDegrees(facetGroup), 0) / 15f)
                : key;
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
                    if (!transformedVector.EqualsWithinTolerance(vb, 0.001f))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool GetPossibleAtoBTransform(RvmFacetGroup aFacetGroup, RvmFacetGroup bFacetGroup,
        out Matrix4x4 transform)
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