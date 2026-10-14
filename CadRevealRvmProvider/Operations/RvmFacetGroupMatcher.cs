namespace CadRevealRvmProvider.Operations;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CadRevealComposer.Utils;
using Commons.Utils;
using RvmSharp.Operations;
using RvmSharp.Primitives;

public static class RvmFacetGroupMatcher
{
    public abstract record Result(RvmFacetGroup FacetGroup);

    public record NotInstancedResult(RvmFacetGroup FacetGroup) : Result(FacetGroup);

    public record InstancedResult(RvmFacetGroup FacetGroup, RvmFacetGroup Template, Matrix4x4 Transform)
        : Result(FacetGroup);

    public record TemplateResult(RvmFacetGroup FacetGroup, RvmFacetGroup Template, Matrix4x4 Transform)
        : InstancedResult(FacetGroup, Template, Transform);

    private class TemplateItem(RvmFacetGroup original, RvmFacetGroup template, Matrix4x4 transform)
    {
        public RvmFacetGroup Original { get; } = original;
        public RvmFacetGroup Template { get; } = template;
        public Matrix4x4 Transform { get; } = transform;
        public PreparedMatch Matching { get; } = new PreparedMatch(ReadPositions(template));
        public int MatchCount { get; set; }
        public int MatchAttempts { get; set; }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static RvmFacetGroup BakeTransformAndCenter(
        RvmFacetGroup facetGroup,
        bool centerMesh,
        out Matrix4x4 translationMatrix
    )
    {
        var originalMatrix = facetGroup.Matrix;

        // Calculate bounds for new bounding box
        var minBounds = new Vector3(float.MaxValue);
        var maxBounds = new Vector3(float.MinValue);
        foreach (var polygon in facetGroup.Polygons)
        {
            foreach (var contour in polygon.Contours)
            {
                foreach (var (vertex, _) in contour.Vertices)
                {
                    var transformedVertex = Vector3.Transform(vertex, originalMatrix);
                    minBounds = Vector3.Min(transformedVertex, minBounds);
                    maxBounds = Vector3.Max(transformedVertex, maxBounds);
                }
            }
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

        return facetGroup.TransformVertexData(finalMatrix) with
        {
            BoundingBoxLocal = new RvmBoundingBox(minBounds, maxBounds),
            Matrix = Matrix4x4.Identity,
        };
    }

    private static void PrintTemplateStats(IEnumerable<IGrouping<RvmFacetGroup, InstancedResult>> instanceGroups)
    {
        uint templateIndex = 0;
        var templateStats = instanceGroups.Select(t =>
            (
                templateIndex++,
                t.Count(),
                t.First().FacetGroup.Polygons.Count(),
                t.First().FacetGroup.Polygons.Sum(p => p.Contours.Sum(c => c.Vertices.Count()))
            )
        );

        using (new TeamCityLogBlock("Template Candidate Stats"))
        {
            Console.WriteLine("Template Id, Instance Count, Polygon Count, Total Instances Vertex Count");
            foreach (var (index, instCount, polyCount, vertexCount) in templateStats)
            {
                Console.WriteLine($"{index},{instCount},{polyCount},{vertexCount},{instCount * vertexCount}");
            }
        }

        // This is kept for local testing
        // Uncomment if you want to print a csv with debug data to your local machine
        //using (var writer = new StreamWriter(File.Create("stats.csv")))
        //{
        //    writer.WriteLine("Template TreeIndex,instance count,polygon count,vertex count,total vertex count");
        //    foreach (var (index, instCount, polyCount, vertexCount) in templateStats)
        //    {
        //        writer.WriteLine($"{index},{instCount},{polyCount},{vertexCount},{instCount* vertexCount}");
        //    }
        //}
    }

    /// <summary>
    /// Possibly reduces the number of templates:
    /// the total number of template groups has a limit, because:
    /// templating bears extra cost connected to retrieving the actual geometry when its instance is used
    /// and cost and gain must be in balance
    /// therefore we iterate over the template results and do prioritization based on gain
    /// </summary>
    /// <param name="candidates"></param>
    /// <param name="maxNoTemplates"></param>
    /// <returns></returns>
    private static Result[] ReduceNumberOfTemplates(Result[] candidates, uint maxNoTemplates)
    {
        // groups that are not an instance of a template (regular groups) are copied over as is
        var resultAfterTemplatePrioritization = candidates.Where(x => x is not InstancedResult).ToList();

        // template groups are sorted according to number of instances x vertices and the top N are taken
        // the template groups that did not make after the prioritazion will become regular groups

        // first: get the templates that are instanced
        var resultsWithTemplate = candidates.OfType<InstancedResult>().ToList();
        // second: sort them according to number of instaces x number of vertices in the template mesh
        var instanceGroups = resultsWithTemplate
            .GroupBy(r => r.Template)
            .OrderByDescending(g =>
                g.Count() * g.First().FacetGroup.Polygons.Sum(p => p.Contours.Sum(c => c.Vertices.Count()))
            )
            .ToArray();

        // third: pick N that bring most gain
        int counterTemplates = 0;
        foreach (var instanceGroup in instanceGroups)
        {
            var shouldTemplateGroup = counterTemplates < maxNoTemplates;
            counterTemplates++;

            foreach (var instancedResult in instanceGroup)
            {
                resultAfterTemplatePrioritization.Add(
                    shouldTemplateGroup ? instancedResult : new NotInstancedResult(instancedResult.FacetGroup)
                );
            }
        }

        PrintTemplateStats(instanceGroups);

        return resultAfterTemplatePrioritization.ToArray();
    }

    public static Result[] MatchAll(
        RvmFacetGroup[] allFacetGroups,
        Func<RvmFacetGroup[], bool> shouldMakeTemplateOf,
        uint maxNoTemplates
    )
    {
        var groupingTimer = Stopwatch.StartNew();
        var groupedFacetGroups = allFacetGroups
            .AsParallel()
            .GroupBy(CalculateKey)
            .Select(g =>
                g.OrderByDescending(x =>
                        // This has a potential to return 0 if one axis is 0m if this is a problem we maye need to adjust this.
                        x.BoundingBoxLocal.Extents.X
                        * x.BoundingBoxLocal.Extents.Y
                        * x.BoundingBoxLocal.Extents.Z // Use volume to start with the largest parts so we avoid precision issues when scaling the template
                    )
                    .ToArray()
            )
            .ToArray();

        var groupCount = groupedFacetGroups.Count(shouldMakeTemplateOf);
        var facetGroupForMatchingCount = groupedFacetGroups
            .Where(shouldMakeTemplateOf)
            .Sum(facetGroups => facetGroups.Length);
        Console.WriteLine(
            $"Found {groupCount:N0} groups for a count of {facetGroupForMatchingCount:N0} facet groups "
                + $"of total {allFacetGroups.Length:N0} in {groupingTimer.Elapsed}"
        );
        Console.WriteLine("Algorithm is O(n^2) of group size (worst case).");
        Console.WriteLine("Explanations. IC: iteration count, TC: template count, VC: vertex count");

        (IReadOnlyList<Result> Result, long IterationCounter) MatchGroup(
            RvmFacetGroup[] facetGroups,
            FacetGroupMatcherLogObject logObject
        )
        {
            var result = new List<Result>();

            if (shouldMakeTemplateOf(facetGroups) is false)
            {
                foreach (var facetGroup in facetGroups)
                {
                    result.Add(new NotInstancedResult(facetGroup));
                }

                // return early so the group isn't logged to console
                return (result, 0L);
            }

            var timer = Stopwatch.StartNew();
            var matchingResults = MatchFacetGroups(facetGroups, out var iterationCounter);

            // results that do not match and thus cannot be instanced, and are added as is
            result = matchingResults.Where(x => x is not InstancedResult).ToList();

            // post-determine if group is an adequate template candidate
            // criterion evaluated here is that a template should have a minimum number of instances
            // see the definition of shouldMakeTemplateOf
            var templateCount = 0L;
            var instancedCount = 0L;
            var instancedResults = matchingResults.OfType<InstancedResult>().ToList();

            // each group of matching instances will be evaluated as a candidate for a template
            var instanceGroups = instancedResults.GroupBy(r => r.Template);

            foreach (var instancesGroup in instanceGroups)
            {
                // what about instancedResults that are not of type FacetGroup??
                var facetGroup = instancesGroup.Select(x => x.FacetGroup).ToArray();
                var shouldMakeTemplateForGroup = shouldMakeTemplateOf(facetGroup);

                foreach (var instancedResult in instancesGroup)
                {
                    if (shouldMakeTemplateForGroup)
                    {
                        instancedCount++;
                        if (instancedResult is TemplateResult)
                        {
                            templateCount++;
                        }
                    }

                    result.Add(
                        shouldMakeTemplateForGroup
                            ? instancedResult
                            : new NotInstancedResult(instancedResult.FacetGroup)
                    );
                }
            }

            var vertexCount = facetGroups.First().Polygons.Sum(x => x.Contours.Sum(y => y.Vertices.Count));

            logObject.AddFacetGroupMatchingResult(
                instancedCount,
                facetGroups.Length,
                templateCount,
                vertexCount,
                iterationCounter,
                timer.Elapsed.TotalSeconds
            );

            return (result, iterationCounter);
        }

        long iterationCounter = 0;

        var instancingLogObject = new FacetGroupMatcherLogObject();
        var matchingResult = groupedFacetGroups
            .OrderByDescending(facetGroups => facetGroups.Length)
            .AsParallel()
            .SelectMany(x =>
            {
                var result = MatchGroup(x, instancingLogObject);
                Interlocked.Add(ref iterationCounter, result.IterationCounter);
                return result.Result;
            })
            .ToArray();
        instancingLogObject.LogFacetGroupMatchingResults();

        var finalResult = ReduceNumberOfTemplates(matchingResult, maxNoTemplates);

        var templateCount = finalResult.OfType<TemplateResult>().Count();
        var instancedCount = finalResult.OfType<InstancedResult>().Count();
        var fraction = instancedCount / (float)allFacetGroups.Length;
        Console.WriteLine(
            $"Facet groups generated {templateCount:N0} templates representing {instancedCount:N0} instances "
                + $"from a total of {allFacetGroups.Length:N0} ({fraction:P1})."
        );
        Console.WriteLine($"Total iteration count: {iterationCounter}");

        if (finalResult.Length != allFacetGroups.Length)
        {
            throw new Exception(
                $"Input and output count doesn't match up. {allFacetGroups.Length} vs {finalResult.Length}"
            );
        }

        return finalResult;
    }

    private static List<Result> MatchFacetGroups(RvmFacetGroup[] facetGroups, out long iterationCounter)
    {
        var result = new List<Result>();
        var templateCandidates = new List<TemplateItem>(); // sorted high to low by explicit code
        // Each bucket runs independently. Reuse positions across its many template attempts without baking normals,
        // bounds, or a new contour hierarchy for every candidate. Keep world-space positions for the 1 mm tolerance.
        var candidatePositions = Array.Empty<Vector3>();

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
                var vertexCount = facetGroups.First().Polygons.Sum(x => x.Contours.Sum(y => y.Vertices.Count));
                Console.WriteLine(
                    $"Grouping with {vertexCount} vertices taking a long time. More than {(int)target.TotalMinutes} minutes. Group key is: {groupKey}"
                );
                target += TimeSpan.FromMinutes(5);
            }

            var matchFoundFromPreviousTemplates = false;
            if (templateCandidates.Count > 0)
            {
                var positionCount = CountPositions(facetGroup);
                if (candidatePositions.Length != positionCount)
                    candidatePositions = new Vector3[positionCount];
                CopyPositions(facetGroup, candidatePositions, applyMatrix: true);
            }

            for (var i = 0; i < templateCandidates.Count; i++)
            {
                var item = templateCandidates[i];
                item.MatchAttempts++;
                iterCounter++;
                if (!item.Matching.TryMatch(candidatePositions, out var transform))
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
                    (templateCandidates[i], templateCandidates[j]) = (templateCandidates[j], templateCandidates[i]);
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
            // This is potentially a bit lossy, is usually within a few percent of optimal
            const int templateCleanupInterval = 500; // Arbitrarily chosen number
            if (cleanupIntervalCounter > templateCleanupInterval)
            {
                CleanupTemplateCandidates(ref templateCandidates, ref result, facetGroups.Length);
                cleanupIntervalCounter = 0;
            }

            cleanupIntervalCounter++;

            templateCandidates.Add(new TemplateItem(facetGroup, newTemplate, newTransform));
        }

        foreach (var template in templateCandidates)
        {
            Result r =
                template.MatchCount > 0
                    ? new TemplateResult(template.Original, template.Template, template.Transform)
                    : new NotInstancedResult(template.Original);

            result.Add(r);
        }

        iterationCounter = iterCounter;
        return result;
    }

    private static void CleanupTemplateCandidates(
        ref List<TemplateItem> templateCandidates,
        ref List<Result> result,
        int facetGroupsLength
    )
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

            result.AddRange(templatesToGiveUpOn.Select(x => new NotInstancedResult(x.Original)));
            templateCandidates.RemoveAll(x => templatesToGiveUpOn.Contains(x));
        }
    }

    /// <summary>
    /// Identifies a facet group with 2 parallel triangles with 3 rectangular sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecialCaseVolumeTriangle(RvmFacetGroup facetGroup)
    {
        return facetGroup.Polygons.Length == 5
            && facetGroup.Polygons[0].Contours.Count == 1
            && facetGroup.Polygons[1].Contours.Count == 1
            && facetGroup.Polygons[2].Contours.Count == 1
            && facetGroup.Polygons[3].Contours.Count == 1
            && facetGroup.Polygons[4].Contours.Count == 1
            && facetGroup.Polygons[0].Contours[0].Vertices.Count == 3
            && facetGroup.Polygons[1].Contours[0].Vertices.Count == 3
            && facetGroup.Polygons[2].Contours[0].Vertices.Count == 4
            && facetGroup.Polygons[3].Contours[0].Vertices.Count == 4
            && facetGroup.Polygons[4].Contours[0].Vertices.Count == 4;
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
                key = key * hashMultiplier + contours.Count;
                for (var j = 0; j < contours.Count; j++)
                {
                    key = key * hashMultiplier + contours[j].Vertices.Count;
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
    ///
    /// You should probably use <see cref="MatchAll"/> instead.
    /// </summary>
    /// <param name="aFacetGroup"></param>
    /// <param name="bFacetGroup"></param>
    /// <param name="outputTransform"></param>
    /// <returns></returns>
    public static bool Match(RvmFacetGroup aFacetGroup, RvmFacetGroup bFacetGroup, out Matrix4x4 outputTransform)
    {
        return new PreparedMatch(ReadPositions(aFacetGroup)).TryMatch(ReadPositions(bFacetGroup), out outputTransform);
    }

    private static int CountPositions(RvmFacetGroup group)
    {
        var count = 0;
        foreach (var polygon in group.Polygons)
        {
            foreach (var contour in polygon.Contours)
                count = checked(count + contour.Vertices.Count);
        }
        return count;
    }

    private static Vector3[] ReadPositions(RvmFacetGroup group)
    {
        var positions = new Vector3[CountPositions(group)];
        CopyPositions(group, positions, applyMatrix: false);
        return positions;
    }

    private static void CopyPositions(RvmFacetGroup group, Span<Vector3> positions, bool applyMatrix)
    {
        var index = 0;
        // Follow polygon views rather than backing-array order: parsing sorts the views for matching.
        foreach (var polygon in group.Polygons)
        {
            foreach (var contour in polygon.Contours)
            {
                foreach (var (vertex, _) in contour.Vertices)
                {
                    positions[index++] = applyMatrix ? Vector3.Transform(vertex, group.Matrix) : vertex;
                }
            }
        }
    }

    private sealed class PreparedMatch
    {
        private readonly Vector3[] positions;
        private readonly (int First, int Second, int Third, int Fourth) anchors;
        private readonly AlgebraUtils.TransformSource source;
        private readonly bool hasAnchors;

        public PreparedMatch(Vector3[] positions)
        {
            this.positions = positions;
            // Anchor selection depends only on the template. Cache failures too: the current solver cannot
            // match planar/degenerate templates, so rescanning them for every candidate cannot help.
            hasAnchors = TryFindAnchors(positions, out anchors);
            if (hasAnchors)
                source = new AlgebraUtils.TransformSource(
                    positions[anchors.First],
                    positions[anchors.Second],
                    positions[anchors.Third],
                    positions[anchors.Fourth]
                );
        }

        public bool TryMatch(ReadOnlySpan<Vector3> candidate, out Matrix4x4 transform)
        {
            // Position indices assume corresponding polygon/contour topology within each bucket.
            if (
                !hasAnchors
                || candidate.Length != positions.Length
                || !source.TryGetTransform(
                    candidate[anchors.First],
                    candidate[anchors.Second],
                    candidate[anchors.Third],
                    candidate[anchors.Fourth],
                    out transform
                )
            )
            {
                transform = default;
                return false;
            }

            // Anchors propose a transform, not a match. Every position must satisfy the 1 mm tolerance.
            for (var index = 0; index < positions.Length; index++)
            {
                if (!Vector3.Transform(positions[index], transform).EqualsWithinTolerance(candidate[index], 0.001f))
                    return false;
            }
            return true;
        }
    }

    private static bool TryFindAnchors(
        ReadOnlySpan<Vector3> positions,
        out (int First, int Second, int Third, int Fourth) anchors
    )
    {
        var first = -1;
        var second = -1;
        var third = -1;
        for (var index = 0; index < positions.Length; index++)
        {
            var vertex = positions[index];
            const float factor = 0.001f;
            if (
                first >= 0 && positions[first].EqualsWithinFactor(vertex, factor)
                || second >= 0 && positions[second].EqualsWithinFactor(vertex, factor)
                || third >= 0 && positions[third].EqualsWithinFactor(vertex, factor)
            )
                continue;

            if (first < 0)
                first = index;
            else if (second < 0)
                second = index;
            else if (third < 0)
            {
                var direction12 = Vector3.Normalize(positions[second] - positions[first]);
                var direction13 = Vector3.Normalize(vertex - positions[first]);
                if (!Vector3.Cross(direction12, direction13).LengthSquared().ApproximatelyEquals(0f))
                    third = index;
            }
            else
            {
                var firstVertex = positions[first];
                var secondVertex = positions[second];
                var thirdVertex = positions[third];
                // csharpier-ignore -- Keep matrix rows aligned for readability.
                var matrix = new Matrix4x4(
                    firstVertex.X, secondVertex.X, thirdVertex.X, vertex.X,
                    firstVertex.Y, secondVertex.Y, thirdVertex.Y, vertex.Y,
                    firstVertex.Z, secondVertex.Z, thirdVertex.Z, vertex.Z,
                    1, 1, 1, 1);
                if (!matrix.GetDeterminant().ApproximatelyEquals(0, 0.000_001f))
                {
                    anchors = (first, second, third, index);
                    return true;
                }
            }
        }

        anchors = default;
        return false;
    }
}
