namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CadRevealRvmProvider.Operations;
using Containers;
using Primitives;

/// <summary>
/// Measures facet-group reuse across a set of RVM files without tessellating or exporting.
/// Each file is reduced to its strictest classes, which are then clustered across all files
/// for several match modes and tolerances. The matcher used by the exporter runs on the same
/// facet groups, so the report states how many of the groups it leaves unmatched are
/// duplicates under each setting.
/// </summary>
internal static class NativeReuseProbe
{
    private const double ReportedTolerance = 0.0001;

    private static readonly double[] Tolerances = [0.00002, 0.00005, ReportedTolerance, 0.0002, 0.0005, 0.001];

    internal static void Run(
        IReadOnlyList<(string RvmFilename, string? TxtFilename)> workload,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions,
        string outputPath
    )
    {
        var timer = Stopwatch.StartNew();
        var sources = workload.OrderBy(source => source.RvmFilename, StringComparer.Ordinal).ToArray();
        var parallelism = Math.Max(1, Environment.ProcessorCount / 2);
        var perSource = new SourceResult[sources.Length];
        var completed = 0;

        // The existing matcher logs every group it compares; keep that out of the report.
        var console = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            Parallel.For(
                0,
                sources.Length,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                index =>
                {
                    perSource[index] = ReadSource(index, sources[index], attributeExclusions);
                    var done = Interlocked.Increment(ref completed);
                    if (done % 25 == 0 || done == sources.Length)
                        console.WriteLine($"Read {done}/{sources.Length} sources ({timer.Elapsed.TotalSeconds:F0}s)");
                }
            );
        }
        finally
        {
            Console.SetOut(console);
        }

        var representatives = perSource.SelectMany(source => source.Representatives).ToList();
        var details = perSource.SelectMany(source => source.Details).ToDictionary(pair => pair.Key, pair => pair.Value);
        var totals = new
        {
            sources = sources.Length,
            facetGroups = perSource.Sum(source => (long)source.FacetGroups),
            facetVertices = perSource.Sum(source => source.Vertices),
            boxLikeFacetGroups = perSource.Sum(source => (long)source.BoxLike),
            existingMatcherUnmatched = perSource.Sum(source => (long)source.Unmatched),
            existingMatcherUnmatchedWithInFileRigidCopy = perSource.Sum(source => (long)source.UnmatchedWithCopy),
            perFileStrictClasses = representatives.Count,
            strictToleranceMm = Tolerances[0] * 1000,
        };
        Console.WriteLine(
            $"{totals.sources} sources, {totals.facetGroups:N0} facet groups "
                + $"({totals.boxLikeFacetGroups:N0} box-like), {totals.facetVertices:N0} contour vertices "
                + $"({timer.Elapsed.TotalSeconds:F0}s)"
        );
        Console.WriteLine(
            $"Existing matcher leaves {totals.existingMatcherUnmatched:N0} non-box facet groups "
                + $"unmatched; {totals.existingMatcherUnmatchedWithInFileRigidCopy:N0} of them have a rigid copy "
                + $"in the same file at {totals.strictToleranceMm:0.###} mm"
        );

        var settings = new List<SettingResult>();
        List<FacetClass>? reported = null;
        // Uniform scaling is left out: on a 44-source sample it merged 7% more classes than rigid
        // matching and was two orders of magnitude slower than every other setting.
        foreach (
            var (mode, tolerances) in new (FacetMatchMode, double[])[]
            {
                (FacetMatchMode.Rigid, Tolerances),
                (FacetMatchMode.RigidOrMirrored, [ReportedTolerance]),
                (FacetMatchMode.Affine, Tolerances),
            }
        )
        foreach (var tolerance in tolerances)
        {
            var started = timer.Elapsed;
            var classes = NativeFacetMatcher.Cluster(representatives, mode, tolerance, parallelism);
            settings.Add(
                Summarize(mode.ToString(), tolerance, classes, details, (timer.Elapsed - started).TotalSeconds)
            );
            if (mode == FacetMatchMode.Rigid && tolerance == ReportedTolerance)
                reported = classes;
        }
        foreach (var tolerance in Tolerances)
        {
            var started = timer.Elapsed;
            var classes = UnorderedLowerBound(representatives, tolerance, parallelism);
            settings.Add(
                Summarize("UnorderedLowerBound", tolerance, classes, details, (timer.Elapsed - started).TotalSeconds)
            );
        }

        Console.WriteLine();
        Console.WriteLine(
            "Non-box facet groups only; 'recovered' counts groups the existing matcher left "
                + "unmatched that share a template here."
        );
        Console.WriteLine(
            $"{"mode", -20}{"tol mm", 8}{"classes", 11}{"single-use", 12}{"cross-file", 12}"
                + $"{"tmpl verts", 13}{"recovered", 11}{"still alone", 13}{"sec", 7}"
        );
        foreach (var setting in settings)
            Console.WriteLine(
                $"{setting.Mode, -20}{setting.ToleranceMm, 8:0.###}{setting.Classes, 11:N0}"
                    + $"{setting.SingleUse, 12:N0}{setting.CrossFile, 12:N0}{setting.TemplateVertices, 13:N0}"
                    + $"{setting.UnmatchedRecovered, 11:N0}{setting.UnmatchedAlone, 13:N0}{setting.Seconds, 7:F1}"
            );

        var top = (reported ?? [])
            .Where(group => !details[group.Representative].BoxLike)
            .Select(group => new TopClass(
                group.Members.Sum(member => member.Shape.Weight),
                group.Members.Select(member => member.Shape.Source).Distinct().Count(),
                group.Representative.VertexCount,
                group.Representative.Radius,
                Path.GetFileNameWithoutExtension(sources[group.Representative.Source].RvmFilename)
            ))
            .OrderByDescending(entry => entry.Uses)
            .ThenBy(entry => entry.ExampleSource, StringComparer.Ordinal)
            .Take(20)
            .ToArray();
        Console.WriteLine();
        Console.WriteLine($"Most reused facet templates (rigid, {ReportedTolerance * 1000:0.###} mm):");
        foreach (var entry in top)
            Console.WriteLine(
                $"  used {entry.Uses, 8:N0} times across {entry.SourceFiles, 4} file(s); "
                    + $"{entry.Vertices, 5} vertices, radius {entry.RadiusMetres:0.####} m, e.g. {entry.ExampleSource}"
            );

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        using var stream = File.Create(outputPath);
        JsonSerializer.Serialize(
            stream,
            new
            {
                totals,
                settings,
                mostReused = top,
                elapsedSeconds = timer.Elapsed.TotalSeconds,
            },
            new JsonSerializerOptions { WriteIndented = true }
        );
        Console.WriteLine($"Probe report: {outputPath} ({timer.Elapsed.TotalSeconds:F0}s)");
    }

    private static SourceResult ReadSource(
        int index,
        (string RvmFilename, string? TxtFilename) source,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions
    )
    {
        RvmFile rvmFile;
        using (var stream = File.OpenRead(source.RvmFilename))
            rvmFile = RvmParser.ReadRvm(stream);
        if (!string.IsNullOrEmpty(source.TxtFilename))
            rvmFile.AttachAttributes(source.TxtFilename);

        var facetGroups = new List<RvmFacetGroup>();
        foreach (var rootChild in rvmFile.Model.Children)
            Collect(rootChild);

        // Same call the exporter makes, so "unmatched" means what ends up in the remains OBJ.
        var matched = new HashSet<RvmFacetGroup>(ReferenceEqualityComparer.Instance);
        if (facetGroups.Count > 1)
        {
            foreach (
                var result in RvmFacetGroupMatcher
                    .MatchAll(facetGroups.ToArray(), static group => group.Length > 1, uint.MaxValue)
                    .OfType<RvmFacetGroupMatcher.InstancedResult>()
            )
                matched.Add(result.FacetGroup);
        }

        var shapes = new List<FacetShape>();
        var boxLike = new bool[facetGroups.Count];
        long vertices = 0;
        for (var ordinal = 0; ordinal < facetGroups.Count; ordinal++)
        {
            var shape = FacetShape.Create(facetGroups[ordinal], index, ordinal);
            if (shape == null)
                continue;
            vertices += shape.VertexCount;
            boxLike[ordinal] = IsBoxLike(facetGroups[ordinal], shape);
            shapes.Add(shape);
        }

        var representatives = new List<FacetShape>();
        var details = new List<KeyValuePair<FacetShape, Detail>>();
        var unmatchedTotal = 0;
        var unmatchedWithCopy = 0;
        foreach (var group in NativeFacetMatcher.Cluster(shapes, FacetMatchMode.Rigid, Tolerances[0], 1))
        {
            var box = boxLike[group.Representative.Ordinal];
            var unmatched = box
                ? 0
                : group.Members.Count(member => !matched.Contains(facetGroups[member.Shape.Ordinal]));
            unmatchedTotal += unmatched;
            if (group.Members.Count > 1)
                unmatchedWithCopy += unmatched;
            var representative = FacetShape.FromCentered(
                group.Representative.StructureKey,
                group.Representative.Points,
                group.Representative.Center,
                index,
                group.Representative.Ordinal,
                group.Members.Count
            );
            representatives.Add(representative);
            details.Add(new(representative, new Detail(box, unmatched)));
        }
        return new SourceResult(
            facetGroups.Count,
            vertices,
            boxLike.Count(value => value),
            unmatchedTotal,
            unmatchedWithCopy,
            representatives,
            details
        );

        void Collect(RvmGroup group)
        {
            if (group is not RvmNode node || NativeInstanceExporter.IsExcluded(node, attributeExclusions))
                return;
            facetGroups.AddRange(node.Children.OfType<RvmFacetGroup>());
            foreach (var child in node.Children.OfType<RvmNode>())
                Collect(child);
        }
    }

    /// <summary>
    /// Six four-sided faces over eight corners with parallelogram faces. The exporter places
    /// these as unit-cube instances, so they are not part of the template problem.
    /// </summary>
    private static bool IsBoxLike(RvmFacetGroup facetGroup, FacetShape shape)
    {
        if (
            facetGroup.Polygons.Length != 6
            || facetGroup.Polygons.Any(polygon =>
                polygon.Contours.Length != 1 || polygon.Contours[0].Vertices.Length != 4
            )
        )
            return false;

        var points = shape.Points;
        var tolerance = Math.Max(1e-6, shape.Radius * 2e-5);
        for (var face = 0; face < 6; face++)
        for (var axis = 0; axis < 3; axis++)
        {
            var offset = face * 12 + axis;
            if (Math.Abs(points[offset] + points[offset + 6] - points[offset + 3] - points[offset + 9]) > tolerance)
                return false;
        }

        Span<int> corners = stackalloc int[8];
        var cornerCount = 0;
        for (var vertex = 0; vertex < 24; vertex++)
        {
            var known = false;
            for (var corner = 0; corner < cornerCount && !known; corner++)
            {
                var other = corners[corner];
                known =
                    Math.Abs(points[vertex * 3] - points[other * 3]) <= tolerance
                    && Math.Abs(points[vertex * 3 + 1] - points[other * 3 + 1]) <= tolerance
                    && Math.Abs(points[vertex * 3 + 2] - points[other * 3 + 2]) <= tolerance;
            }
            if (known)
                continue;
            if (cornerCount == 8)
                return false;
            corners[cornerCount++] = vertex;
        }
        return cornerCount == 8;
    }

    private static SettingResult Summarize(
        string mode,
        double tolerance,
        List<FacetClass> classes,
        Dictionary<FacetShape, Detail> details,
        double seconds
    )
    {
        long count = 0,
            singleUse = 0,
            crossFile = 0,
            templateVertices = 0,
            recovered = 0,
            alone = 0;
        foreach (var group in classes)
        {
            if (details[group.Representative].BoxLike)
                continue;
            long uses = 0,
                unmatched = 0;
            var many = false;
            foreach (var (shape, _) in group.Members)
            {
                uses += shape.Weight;
                unmatched += details[shape].Unmatched;
                many |= shape.Source != group.Representative.Source;
            }
            count++;
            templateVertices += group.Representative.VertexCount;
            if (many)
                crossFile++;
            if (uses == 1)
            {
                singleUse++;
                alone += unmatched;
            }
            else
            {
                recovered += unmatched;
            }
        }
        return new SettingResult(
            mode,
            tolerance * 1000,
            count,
            singleUse,
            crossFile,
            templateVertices,
            recovered,
            alone,
            seconds
        );
    }

    /// <summary>
    /// Groups shapes whose vertex counts and sorted centroid distances agree, ignoring vertex
    /// order and polygon layout. Congruent parts always agree, so this is the fewest classes any
    /// order-independent rigid matcher could reach; unrelated shapes can agree by coincidence.
    /// </summary>
    private static List<FacetClass> UnorderedLowerBound(List<FacetShape> shapes, double tolerance, int parallelism)
    {
        var buckets = shapes
            .GroupBy(shape => shape.VertexCount)
            .OrderBy(group => group.Key)
            .Select(group =>
                group
                    .OrderBy(shape => shape.Radius)
                    .ThenBy(shape => shape.Source)
                    .ThenBy(shape => shape.Ordinal)
                    .ToArray()
            )
            .ToArray();
        var results = new List<FacetClass>[buckets.Length];
        Parallel.For(
            0,
            buckets.Length,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            index =>
            {
                var classes = new List<FacetClass>();
                var profiles = new List<float[]>();
                foreach (var shape in buckets[index])
                {
                    var profile = (float[])shape.Radial.Clone();
                    Array.Sort(profile);
                    var matched = false;
                    for (var candidate = classes.Count - 1; candidate >= 0; candidate--)
                    {
                        if (shape.Radius - classes[candidate].Representative.Radius > tolerance)
                            break;
                        var other = profiles[candidate];
                        var agree = true;
                        for (var vertex = 0; vertex < profile.Length && agree; vertex++)
                            agree = Math.Abs(profile[vertex] - other[vertex]) <= tolerance;
                        if (!agree)
                            continue;
                        classes[candidate].Members.Add((shape, FacetFit.Identity));
                        matched = true;
                        break;
                    }
                    if (matched)
                        continue;
                    classes.Add(new FacetClass(shape));
                    profiles.Add(profile);
                }
                results[index] = classes;
            }
        );
        return results.SelectMany(classes => classes).ToList();
    }

    private sealed record Detail(bool BoxLike, int Unmatched);

    private sealed record SourceResult(
        int FacetGroups,
        long Vertices,
        int BoxLike,
        int Unmatched,
        int UnmatchedWithCopy,
        List<FacetShape> Representatives,
        List<KeyValuePair<FacetShape, Detail>> Details
    );

    private sealed record SettingResult(
        string Mode,
        double ToleranceMm,
        long Classes,
        long SingleUse,
        long CrossFile,
        long TemplateVertices,
        long UnmatchedRecovered,
        long UnmatchedAlone,
        double Seconds
    );

    private sealed record TopClass(long Uses, int SourceFiles, int Vertices, float RadiusMetres, string ExampleSource);
}
