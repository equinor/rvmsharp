namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Primitives;
using Tessellation;

internal enum PrimitivePartKind : byte
{
    Cube = 0,
    Cylinder = 1,
}

/// <summary>One unit primitive; <see cref="Matrix"/> places it in the template's frame (row vectors).</summary>
internal readonly record struct PrimitivePart(PrimitivePartKind Kind, Matrix4x4 Matrix);

internal sealed record PrimitiveFit(string Replacement, PrimitivePart[] Parts, double VolumeError, double ProfileIoU);

/// <summary>Result of fitting one template: a fit, or the reason the mesh is kept.</summary>
internal readonly record struct PrimitiveOutcome(string Outcome, PrimitiveFit? Fit)
{
    internal static PrimitiveOutcome Kept(string reason) => new("kept:" + reason, null);

    internal static PrimitiveOutcome Replaced(PrimitiveFit fit) => new("replaced", fit);
}

internal sealed record PrimitiveFitOptions
{
    /// <summary>Also split unhinted prisms into boxes; normally only section profiles are.</summary>
    public bool Unhinted { get; init; }

    public double MaxVolumeError { get; init; } = 0.12;

    public double MinProfileIoU { get; init; } = 0.82;

    /// <summary>Largest share of the profile outline that fillets and chamfers may take.</summary>
    public double FilletShare { get; init; } = 0.15;

    /// <summary>Largest slope, in radians, of a tapered flange face that still counts as aligned.</summary>
    public double MaxTaperAngle { get; init; } = 0.2;

    /// <summary>Shortest tapered face, as a share of the profile size; shorter sloped edges are fillets.</summary>
    public double MinTaperLength { get; init; } = 0.15;

    public int MaxParts { get; init; } = 8;

    /// <summary>
    /// Most unit cubes an exact box decomposition may use. Shapes whose faces lie in three
    /// perpendicular directions are split into boxes with no change in shape; 0 disables it.
    /// </summary>
    public int BoxMaxParts { get; init; } = 5;

    /// <summary>
    /// Largest distance, in metres, between a replacement's surface and the source surface where the
    /// replacement sticks out of the source solid. Squared-off mitres, filled notches and holes fail it;
    /// grooves, snipes and fillets pass.
    /// </summary>
    public double MaxGap { get; init; } = 0.005;

    /// <summary>
    /// Largest distance, in metres, from any point of the source surface to the replacement. Root
    /// fillets (about 0.4 x their radius) pass; large rounded cut-outs and gussets fail.
    /// </summary>
    public double MaxMissingGap { get; init; } = 0.02;

    public double CubeMaxMissingFraction { get; init; } = 0.03;

    public double CubeSmallMaxDimension { get; init; } = 0.1;

    public double CubeSmallMaxMissingFraction { get; init; } = 0.09;

    /// <summary>Missing volume a box may stand in for, since the gap check bounds where it is missing.</summary>
    public double NearBoxMaxMissingFraction { get; init; } = 0.08;

    public int CubeMinVertices { get; init; } = 8;

    /// <summary>Safety limit on the size of templates tested as one box.</summary>
    public int CubeMaxVertices { get; init; } = 4096;

    public double ClosedMinAspect { get; init; } = 1.0;

    public double ClosedMinSilhouetteFill { get; init; } = 0.95;
}

internal readonly record struct D3(double X, double Y, double Z)
{
    public static D3 operator +(D3 a, D3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static D3 operator -(D3 a, D3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static D3 operator -(D3 a) => new(-a.X, -a.Y, -a.Z);

    public static D3 operator *(D3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    public static D3 operator /(D3 a, double s) => new(a.X / s, a.Y / s, a.Z / s);

    internal double Dot(D3 o) => X * o.X + Y * o.Y + Z * o.Z;

    internal D3 Cross(D3 o) => new(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);

    internal double Length => Math.Sqrt(Dot(this));

    internal D3 Normalized() => this / Length;

    internal static D3 From(Vector3 v) => new(v.X, v.Y, v.Z);
}

/// <summary>
/// Replaces a facet group by unit boxes or a unit cylinder when that is exact within tolerance.
/// RVM facet groups are planar polygons, so prisms, their profiles and their ends are found
/// directly from the faces; no sampling is involved. The input is the template's polygons and
/// its tessellated mesh in the same frame.
/// </summary>
internal static partial class NativePrimitiveFitter
{
    private const double SideDot = 1e-3;
    private const double EndDot = 0.9999;
    private const double SameDirectionDot = 0.9999;
    private const double PerpendicularDot = 1e-3;
    private const double AngleTolerance = 2e-3;
    private const int MaxDirections = 64;
    private const int IoURows = 512;

    private sealed record Face(D3[][] Contours, D3 Normal, double Area);

    private readonly record struct V2(double X, double Y);

    private sealed record Prism(D3 Axis, D3 U, D3 V, double Min, double Max, List<V2[]> Loops);

    /// <summary>Polygons of a facet group with its matrix applied.</summary>
    internal static D3[][][] Faces(RvmFacetGroup facetGroup)
    {
        var matrix = facetGroup.Matrix;
        return facetGroup
            .Polygons.Select(polygon =>
                polygon
                    .Contours.Select(contour =>
                        contour.Vertices.Select(vertex => D3.From(Vector3.Transform(vertex.Vertex, matrix))).ToArray()
                    )
                    .ToArray()
            )
            .ToArray();
    }

    /// <summary>The facet group's mesh with its matrix applied, in the frame of <see cref="Faces"/>.</summary>
    internal static RvmMesh? Mesh(RvmFacetGroup facetGroup)
    {
        var mesh = TessellatorBridge.TessellateWithoutApplyingMatrix(facetGroup, 1, 0);
        mesh?.Apply(facetGroup.Matrix);
        return mesh;
    }

    internal static PrimitiveOutcome Fit(
        IReadOnlyList<D3[][]> polygons,
        RvmMesh? mesh,
        ProfileFamily hint,
        PrimitiveFitOptions options
    )
    {
        var faces = new List<Face>(polygons.Count);
        foreach (var polygon in polygons)
        {
            var contours = polygon.Where(contour => contour.Length >= 3).ToArray();
            if (contours.Length == 0)
                continue;
            var normal = Newell(contours[0]);
            var length = normal.Length;
            if (length < 1e-18)
                continue;
            faces.Add(new Face(contours, normal / length, length / 2));
        }
        if (faces.Count < 4)
            return PrimitiveOutcome.Kept("degenerate");

        var points = faces.SelectMany(face => face.Contours.SelectMany(contour => contour)).ToArray();
        var (minimum, maximum) = Bounds(points);
        var extent = (maximum - minimum).Length;
        if (!(extent > 0))
            return PrimitiveOutcome.Kept("degenerate");
        var tolerance = Math.Max(1e-5, extent * 1e-5);

        var directions = Directions(faces);
        if (directions == null)
            return PrimitiveOutcome.Kept("curved");

        // Shapes built only from faces in three perpendicular directions are exactly a union of
        // boxes; that exact split wins over every approximate fit below.
        if (options.BoxMaxParts > 0 && TryBoxes(faces, directions, mesh, options.BoxMaxParts, out var boxes))
            return PrimitiveOutcome.Replaced(boxes);

        var prisms = new List<Prism>();
        foreach (var (direction, _) in directions)
        {
            if (TryPrism(faces, points, direction, tolerance, square: true, out var prism))
                prisms.Add(prism);
        }
        if (prisms.Count == 0 && IsLongObliquePrism(faces, points, directions, tolerance))
            return PrimitiveOutcome.Kept("oblique");

        PrimitiveFit? best = null;
        PrimitiveOutcome? rejected = null;
        foreach (var prism in prisms)
        {
            var outcome = FitPrism(prism, hint, options, tolerance);
            if (outcome.Fit is { } candidate && !WithinGap(candidate, mesh, options))
                outcome = PrimitiveOutcome.Kept("gap");
            if (outcome.Fit is not { } fit)
            {
                rejected ??= outcome;
                continue;
            }
            if (
                best == null
                || fit.Parts.Length < best.Parts.Length
                || (fit.Parts.Length == best.Parts.Length && fit.ProfileIoU > best.ProfileIoU)
            )
                best = fit;
        }
        if (best != null)
            return PrimitiveOutcome.Replaced(best);

        var cube = CubeLike(mesh, directions, options, tolerance);
        if (cube.Fit != null && !WithinGap(cube.Fit, mesh, options))
            cube = PrimitiveOutcome.Kept("cube-gap");
        if (cube.Fit != null || rejected == null)
            return cube;
        return new PrimitiveOutcome(rejected.Value.Outcome + "/" + cube.Outcome["kept:".Length..], null);
    }

    /// <summary>
    /// Whether every part's surface, where it lies outside the source solid, stays within
    /// <see cref="PrimitiveFitOptions.MaxGap"/> of the source surface. Faces inside the solid (where
    /// parts meet, or run through a root fillet) are not visible and do not count. Hollow sections
    /// are filled on purpose, so only their side faces are checked.
    /// </summary>
    internal static bool WithinGap(PrimitiveFit fit, RvmMesh? mesh, PrimitiveFitOptions options)
    {
        if (mesh == null || mesh.Triangles.Length < 3)
            return false;
        var lateralOnly = fit.Replacement is "ROUND_HOLLOW" or "CLOSED_SHELL";
        var triangles = new (D3 A, D3 B, D3 C)[mesh.Triangles.Length / 3];
        for (var index = 0; index < triangles.Length; index++)
            triangles[index] = (
                D3.From(mesh.Vertices[mesh.Triangles[index * 3]]),
                D3.From(mesh.Vertices[mesh.Triangles[index * 3 + 1]]),
                D3.From(mesh.Vertices[mesh.Triangles[index * 3 + 2]])
            );
        var limit = options.MaxGap * options.MaxGap;
        foreach (var part in fit.Parts)
        {
            foreach (var point in SurfaceSamples(part, lateralOnly, options.MaxGap))
            {
                if (!WithinDistance(point, triangles, limit) && Math.Abs(Winding(point, triangles)) < 0.5)
                    return false;
            }
        }
        if (lateralOnly)
            return true;
        // The other way round: no part of the source surface may be far from the replacement, so
        // large rounded cut-outs are not filled in as if they were root fillets.
        foreach (var (a, b, c) in triangles)
        {
            foreach (var point in TriangleSamples(a, b, c))
            {
                var nearest = double.MaxValue;
                foreach (var part in fit.Parts)
                    nearest = Math.Min(nearest, DistanceToPartSurface(point, part));
                if (nearest > options.MaxMissingGap)
                    return false;
            }
        }
        return true;
    }

    /// <summary>Corners, edge points and interior points of a triangle, about 10 mm apart.</summary>
    private static IEnumerable<D3> TriangleSamples(D3 a, D3 b, D3 c)
    {
        var longest = Math.Max((b - a).Length, Math.Max((c - b).Length, (a - c).Length));
        var steps = Math.Clamp((int)Math.Ceiling(longest / 0.01), 1, 128);
        for (var i = 0; i <= steps; i++)
        for (var j = 0; i + j <= steps; j++)
            yield return a + (b - a) * ((double)i / steps) + (c - a) * ((double)j / steps);
    }

    /// <summary>Distance from a point to the surface of a unit box or cylinder part.</summary>
    private static double DistanceToPartSurface(D3 point, PrimitivePart part)
    {
        var m = part.Matrix;
        var axes = new[] { new D3(m.M11, m.M12, m.M13), new D3(m.M21, m.M22, m.M23), new D3(m.M31, m.M32, m.M33) };
        var offset = point - new D3(m.M41, m.M42, m.M43);
        var local = new double[3];
        var sizes = new double[3];
        for (var axis = 0; axis < 3; axis++)
        {
            sizes[axis] = axes[axis].Length;
            local[axis] = offset.Dot(axes[axis]) / sizes[axis];
        }
        var halfHeight = sizes[2] / 2;
        var outsideZ = Math.Max(Math.Abs(local[2]) - halfHeight, 0);
        if (part.Kind == PrimitivePartKind.Cylinder)
        {
            // Unit radius 1: the x axis length is the radius.
            var radial = Math.Sqrt(local[0] * local[0] + local[1] * local[1]);
            var outsideRadial = Math.Max(radial - sizes[0], 0);
            if (outsideRadial > 0 || outsideZ > 0)
                return Math.Sqrt(outsideRadial * outsideRadial + outsideZ * outsideZ);
            return Math.Min(sizes[0] - radial, halfHeight - Math.Abs(local[2]));
        }
        double outside = 0,
            inside = double.MaxValue;
        for (var axis = 0; axis < 3; axis++)
        {
            var excess = Math.Abs(local[axis]) - sizes[axis] / 2;
            outside += Math.Max(excess, 0) * Math.Max(excess, 0);
            inside = Math.Min(inside, -excess);
        }
        return outside > 0 ? Math.Sqrt(outside) : inside;
    }

    /// <summary>Grid samples over a unit part's surface, corners and edges included, about <paramref name="spacing"/> apart.</summary>
    private static IEnumerable<D3> SurfaceSamples(PrimitivePart part, bool lateralOnly, double spacing)
    {
        var m = part.Matrix;
        var x = new D3(m.M11, m.M12, m.M13);
        var y = new D3(m.M21, m.M22, m.M23);
        var z = new D3(m.M31, m.M32, m.M33);
        var origin = new D3(m.M41, m.M42, m.M43);
        D3 At(double a, double b, double c) => origin + x * a + y * b + z * c;
        int Count(D3 axis) => Math.Clamp((int)Math.Ceiling(axis.Length / spacing) + 1, 2, 1025);
        var counts = new[] { Count(x), Count(y), Count(z) };
        if (part.Kind == PrimitivePartKind.Cube)
        {
            for (var normal = 0; normal < 3; normal++)
            {
                if (lateralOnly && normal == 2)
                    continue;
                var (first, second) = ((normal + 1) % 3, (normal + 2) % 3);
                for (var side = -1; side <= 1; side += 2)
                for (var i = 0; i < counts[first]; i++)
                for (var j = 0; j < counts[second]; j++)
                {
                    var local = new double[3];
                    local[normal] = side * 0.5;
                    local[first] = -0.5 + (double)i / (counts[first] - 1);
                    local[second] = -0.5 + (double)j / (counts[second] - 1);
                    yield return At(local[0], local[1], local[2]);
                }
            }
            yield break;
        }

        // The unit cylinder has radius 1 about local z and height 1.
        var around = Math.Clamp((int)Math.Ceiling(2 * Math.PI * Math.Max(x.Length, y.Length) / spacing), 16, 512);
        for (var step = 0; step < around; step++)
        {
            var angle = 2 * Math.PI * step / around;
            var (cos, sin) = (Math.Cos(angle), Math.Sin(angle));
            for (var level = 0; level < counts[2]; level++)
                yield return At(cos, sin, -0.5 + (double)level / (counts[2] - 1));
            if (lateralOnly)
                continue;
            for (var ring = 0; ring < 4; ring++)
            {
                var radius = ring / 4.0;
                yield return At(cos * radius, sin * radius, -0.5);
                yield return At(cos * radius, sin * radius, 0.5);
            }
        }
    }

    private static bool WithinDistance(D3 p, (D3 A, D3 B, D3 C)[] triangles, double squaredLimit)
    {
        foreach (var (a, b, c) in triangles)
        {
            var offset = p - ClosestPoint(p, a, b, c);
            if (offset.Dot(offset) <= squaredLimit)
                return true;
        }
        return false;
    }

    /// <summary>Closest point on a triangle (Ericson, Real-Time Collision Detection 5.1.5).</summary>
    private static D3 ClosestPoint(D3 p, D3 a, D3 b, D3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var ap = p - a;
        double d1 = ab.Dot(ap),
            d2 = ac.Dot(ap);
        if (d1 <= 0 && d2 <= 0)
            return a;
        var bp = p - b;
        double d3 = ab.Dot(bp),
            d4 = ac.Dot(bp);
        if (d3 >= 0 && d4 <= d3)
            return b;
        var vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
            return a + ab * (d1 / (d1 - d3));
        var cp = p - c;
        double d5 = ab.Dot(cp),
            d6 = ac.Dot(cp);
        if (d6 >= 0 && d5 <= d6)
            return c;
        var vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
            return a + ac * (d2 / (d2 - d6));
        var va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        var denominator = va + vb + vc;
        if (denominator == 0)
            return a;
        return a + ab * (vb / denominator) + ac * (vc / denominator);
    }

    /// <summary>Generalised winding number of a closed triangle mesh about a point (±1 inside, 0 outside).</summary>
    private static double Winding(D3 p, (D3 A, D3 B, D3 C)[] triangles)
    {
        double total = 0;
        foreach (var (a0, b0, c0) in triangles)
        {
            var a = a0 - p;
            var b = b0 - p;
            var c = c0 - p;
            double la = a.Length,
                lb = b.Length,
                lc = c.Length;
            var numerator = a.Dot(b.Cross(c));
            var denominator = la * lb * lc + a.Dot(b) * lc + b.Dot(c) * la + c.Dot(a) * lb;
            total += 2 * Math.Atan2(numerator, denominator);
        }
        return total / (4 * Math.PI);
    }

    private static D3 Newell(D3[] contour)
    {
        double x = 0,
            y = 0,
            z = 0;
        for (var index = 0; index < contour.Length; index++)
        {
            var a = contour[index];
            var b = contour[(index + 1) % contour.Length];
            x += (a.Y - b.Y) * (a.Z + b.Z);
            y += (a.Z - b.Z) * (a.X + b.X);
            z += (a.X - b.X) * (a.Y + b.Y);
        }
        return new D3(x, y, z);
    }

    private static (D3 Minimum, D3 Maximum) Bounds(IEnumerable<D3> points)
    {
        var minimum = new D3(double.MaxValue, double.MaxValue, double.MaxValue);
        var maximum = new D3(double.MinValue, double.MinValue, double.MinValue);
        foreach (var point in points)
        {
            minimum = new D3(Math.Min(minimum.X, point.X), Math.Min(minimum.Y, point.Y), Math.Min(minimum.Z, point.Z));
            maximum = new D3(Math.Max(maximum.X, point.X), Math.Max(maximum.Y, point.Y), Math.Max(maximum.Z, point.Z));
        }
        return (minimum, maximum);
    }

    /// <summary>Distinct face directions (up to sign) by total area, or null for curved shapes.</summary>
    private static List<(D3 Direction, double Area)>? Directions(List<Face> faces)
    {
        var directions = new List<(D3 Direction, double Area)>();
        foreach (var face in faces)
        {
            var index = directions.FindIndex(entry => Math.Abs(entry.Direction.Dot(face.Normal)) > SameDirectionDot);
            if (index >= 0)
            {
                directions[index] = (directions[index].Direction, directions[index].Area + face.Area);
                continue;
            }
            if (directions.Count == MaxDirections)
                return null;
            directions.Add((face.Normal, face.Area));
        }
        return directions.OrderByDescending(entry => entry.Area).ToList();
    }

    private static D3 Perpendicular(D3 axis)
    {
        var helper = Math.Abs(axis.X) < 0.9 ? new D3(1, 0, 0) : new D3(0, 1, 0);
        return helper.Cross(axis).Normalized();
    }

    /// <summary>
    /// Whether the faces form a prism along <paramref name="axis"/>: every side face is parallel to
    /// the axis and projects to an edge of the profile, and every vertex is a profile vertex at
    /// one of the two ends. With <paramref name="square"/>, the ends must be perpendicular to the axis.
    /// </summary>
    private static bool TryPrism(List<Face> faces, D3[] points, D3 axis, double tolerance, bool square, out Prism prism)
    {
        prism = null!;
        var side = new bool[faces.Count];
        int sides = 0,
            ends = 0;
        for (var index = 0; index < faces.Count; index++)
        {
            var dot = Math.Abs(faces[index].Normal.Dot(axis));
            if (dot < SideDot)
            {
                side[index] = true;
                sides++;
            }
            else if (square && dot < EndDot)
                return false;
            else
                ends++;
        }
        if (sides < 3 || ends < 2)
            return false;

        double low = double.MaxValue,
            high = double.MinValue;
        foreach (var point in points)
        {
            var height = point.Dot(axis);
            low = Math.Min(low, height);
            high = Math.Max(high, height);
        }
        if (high - low <= tolerance)
            return false;
        if (square)
        {
            foreach (var point in points)
            {
                var height = point.Dot(axis);
                if (Math.Abs(height - low) > tolerance && Math.Abs(height - high) > tolerance)
                    return false;
            }
        }

        var u = Perpendicular(axis);
        var v = axis.Cross(u);
        var vertices = new Welder(tolerance);
        var profile = new Welder(tolerance);
        var profileIds = new int[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            vertices.Add(points[index]);
            profileIds[index] = profile.Add(new D3(points[index].Dot(u), points[index].Dot(v), 0));
        }
        if (vertices.Count != 2 * profile.Count)
            return false;

        var segments = new HashSet<(int, int)>();
        var offset = 0;
        for (var index = 0; index < faces.Count; index++)
        {
            var count = faces[index].Contours.Sum(contour => contour.Length);
            if (side[index] && !AddSegments(profileIds.AsSpan(offset, count), profile, tolerance, segments))
                return false;
            offset += count;
        }

        var loops = Chain(segments, profile.Count);
        if (loops == null)
            return false;
        prism = new Prism(
            axis,
            u,
            v,
            low,
            high,
            loops
                .Select(loop => loop.Select(id => new V2(profile.Points[id].X, profile.Points[id].Y)).ToArray())
                .ToList()
        );
        return true;
    }

    /// <summary>A side face must project to one straight profile edge, possibly split by collinear vertices.</summary>
    private static bool AddSegments(
        ReadOnlySpan<int> ids,
        Welder profile,
        double tolerance,
        HashSet<(int, int)> segments
    )
    {
        var distinct = new List<int>();
        foreach (var id in ids)
        {
            if (!distinct.Contains(id))
                distinct.Add(id);
        }
        if (distinct.Count < 2)
            return false;

        var start = profile.Points[distinct[0]];
        var end = distinct.Select(id => profile.Points[id]).MaxBy(point => (point - start).Length);
        var direction = (end - start).Normalized();
        start = distinct.Select(id => profile.Points[id]).MinBy(point => (point - end).Dot(direction));
        var ordered = distinct.OrderBy(id => (profile.Points[id] - start).Dot(direction)).ToArray();
        foreach (var id in ordered)
        {
            var offset = profile.Points[id] - start;
            if ((offset - direction * offset.Dot(direction)).Length > tolerance)
                return false;
        }
        for (var index = 1; index < ordered.Length; index++)
        {
            var (a, b) = (ordered[index - 1], ordered[index]);
            segments.Add(a < b ? (a, b) : (b, a));
        }
        return true;
    }

    private static List<int[]>? Chain(HashSet<(int, int)> segments, int count)
    {
        var adjacency = new List<int>?[count];
        foreach (var (a, b) in segments)
        {
            (adjacency[a] ??= new List<int>(2)).Add(b);
            (adjacency[b] ??= new List<int>(2)).Add(a);
        }
        if (adjacency.Any(neighbours => neighbours is not { Count: 2 }))
            return null;

        var loops = new List<int[]>();
        var seen = new bool[count];
        for (var start = 0; start < count; start++)
        {
            if (seen[start])
                continue;
            var loop = new List<int> { start };
            seen[start] = true;
            int previous = -1,
                current = start;
            while (true)
            {
                var next = adjacency[current]![0] != previous ? adjacency[current]![0] : adjacency[current]![1];
                if (next == start)
                    break;
                if (seen[next])
                    return null;
                loop.Add(next);
                seen[next] = true;
                (previous, current) = (current, next);
            }
            loops.Add(loop.ToArray());
        }
        return loops;
    }

    /// <summary>Whether this is a long prism with mitred ends; those are never replaced.</summary>
    private static bool IsLongObliquePrism(
        List<Face> faces,
        D3[] points,
        List<(D3 Direction, double Area)> directions,
        double tolerance
    )
    {
        var ranked = directions.Take(8).Select(entry => entry.Direction).ToArray();
        for (var first = 0; first < ranked.Length; first++)
        for (var second = first + 1; second < ranked.Length; second++)
        {
            var axis = ranked[first].Cross(ranked[second]);
            if (axis.Length < 0.1)
                continue;
            axis = axis.Normalized();
            // A mitred prism has at least one end face that is neither a side nor square to the
            // axis; box-built shapes with steps along the axis are not mitred.
            if (!faces.Any(face => Math.Abs(face.Normal.Dot(axis)) is >= SideDot and < EndDot))
                continue;
            if (!TryPrism(faces, points, axis, tolerance, square: false, out var prism))
                continue;
            var outline = prism.Loops.SelectMany(loop => loop).ToArray();
            var width = outline.Max(point => point.X) - outline.Min(point => point.X);
            var height = outline.Max(point => point.Y) - outline.Min(point => point.Y);
            if (prism.Max - prism.Min >= 2 * Math.Max(width, height))
                return true;
        }
        return false;
    }

    private static PrimitiveOutcome FitPrism(
        Prism prism,
        ProfileFamily hint,
        PrimitiveFitOptions options,
        double tolerance
    )
    {
        var loops = prism.Loops.Select(loop => Simplify(loop, tolerance)).ToList();
        var outerIndex = Enumerable.Range(0, loops.Count).MaxBy(index => Math.Abs(SignedArea(loops[index])));
        var outer = loops[outerIndex];
        var outerArea = Math.Abs(SignedArea(outer));
        var holes = loops.Count - 1;
        if (holes > 1)
            return PrimitiveOutcome.Kept("multi-loop");
        if (outer.Length < 3 || !(outerArea > 0))
            return PrimitiveOutcome.Kept("degenerate");

        var length = prism.Max - prism.Min;
        var middle = (prism.Max + prism.Min) / 2;
        var hinted = hint != ProfileFamily.None || options.Unhinted;

        if (TryRound(outer, tolerance, out var centre, out var radius))
        {
            if (holes == 1 && !hinted)
                return PrimitiveOutcome.Kept("hollow-round-unhinted");
            // Like closed shells, a hollow section is only filled when it is long; a short one is a
            // ring or collar whose opening is plain to see.
            if (holes == 1 && length <= options.ClosedMinAspect * 4 * radius)
                return PrimitiveOutcome.Kept("hollow-short");
            var circle = Math.PI * radius * radius;
            var volumeError = Math.Abs(circle - outerArea) / outerArea;
            if (volumeError > options.MaxVolumeError)
                return PrimitiveOutcome.Kept("round-volume");
            var origin = prism.U * centre.X + prism.V * centre.Y + prism.Axis * middle;
            var part = new PrimitivePart(
                PrimitivePartKind.Cylinder,
                Placement(prism.U, radius, prism.V, radius, prism.Axis, length, origin)
            );
            return PrimitiveOutcome.Replaced(
                new PrimitiveFit(holes == 1 ? "ROUND_HOLLOW" : "ROUND", [part], volumeError, outerArea / circle)
            );
        }

        if (holes == 1)
        {
            // A closed rectangular shell (RHS/SHS) becomes its outer box, as the Blender pass does:
            // the outline must fill its bounding rectangle, so rounded corners are fine.
            if (!(hint is ProfileFamily.None or ProfileFamily.Any or ProfileFamily.Hollow))
                return PrimitiveOutcome.Kept("hollow-family");
            var shellAngle = MainAngle(outer);
            var shell = Rotate(outer, shellAngle);
            var rectangle = (
                S0: shell.Min(point => point.X),
                S1: shell.Max(point => point.X),
                T0: shell.Min(point => point.Y),
                T1: shell.Max(point => point.Y)
            );
            var width = rectangle.S1 - rectangle.S0;
            var height = rectangle.T1 - rectangle.T0;
            if (length <= options.ClosedMinAspect * (width + height))
                return PrimitiveOutcome.Kept("hollow-short");
            var fill = outerArea / (width * height);
            if (fill < options.ClosedMinSilhouetteFill)
                return PrimitiveOutcome.Kept("hollow-fill");
            var shellS = prism.U * Math.Cos(shellAngle) + prism.V * Math.Sin(shellAngle);
            var shellT = prism.U * -Math.Sin(shellAngle) + prism.V * Math.Cos(shellAngle);
            return PrimitiveOutcome.Replaced(
                new PrimitiveFit(
                    "CLOSED_SHELL",
                    [
                        new PrimitivePart(
                            PrimitivePartKind.Cube,
                            Box(rectangle, shellS, shellT, prism.Axis, length, middle)
                        ),
                    ],
                    1 - fill,
                    fill
                )
            );
        }

        if (!TrySnap(outer, tolerance, options, out var corners, out var angle, out var snapFailure))
            return PrimitiveOutcome.Kept("not-rectilinear-" + snapFailure);
        var rotated = Rotate(outer, angle);
        var s = prism.U * Math.Cos(angle) + prism.V * Math.Sin(angle);
        var t = prism.U * -Math.Sin(angle) + prism.V * Math.Cos(angle);

        if (!hinted)
            return PrimitiveOutcome.Kept("unhinted");
        var label = corners.Length switch
        {
            4 => "RECT",
            6 => "L",
            8 => "CTZ",
            12 => "IH",
            _ => "GENERAL",
        };
        if (!options.Unhinted && !Accepts(hint, label))
            return PrimitiveOutcome.Kept("family");

        var snappedArea = Math.Abs(SignedArea(corners));
        var error = Math.Abs(snappedArea - outerArea) / outerArea;
        if (error > options.MaxVolumeError)
            return PrimitiveOutcome.Kept("volume");
        var iou = IoU(rotated, corners);
        if (iou < options.MinProfileIoU)
            return PrimitiveOutcome.Kept("profile-iou");
        if (!BoundsAgree(rotated, corners, tolerance))
            return PrimitiveOutcome.Kept("bounds");

        var rectangles = Partition(corners, tolerance);
        if (rectangles.Count > options.MaxParts)
            return PrimitiveOutcome.Kept("parts");
        var parts = rectangles
            .Select(rectangle => new PrimitivePart(
                PrimitivePartKind.Cube,
                Box(rectangle, s, t, prism.Axis, length, middle)
            ))
            .ToArray();
        return PrimitiveOutcome.Replaced(new PrimitiveFit("BEAM_" + label, parts, error, iou));
    }

    private static bool Accepts(ProfileFamily hint, string label) =>
        hint switch
        {
            ProfileFamily.Any => true,
            ProfileFamily.IH => label is "IH" or "RECT",
            ProfileFamily.L => label is "L" or "RECT",
            ProfileFamily.C or ProfileFamily.T => label is "CTZ" or "RECT",
            ProfileFamily.Hollow or ProfileFamily.Solid => label is "RECT",
            _ => false,
        };

    private static Matrix4x4 Box(
        (double S0, double S1, double T0, double T1) rectangle,
        D3 s,
        D3 t,
        D3 axis,
        double length,
        double middle
    )
    {
        var origin =
            s * ((rectangle.S0 + rectangle.S1) / 2) + t * ((rectangle.T0 + rectangle.T1) / 2) + axis * middle;
        return Placement(s, rectangle.S1 - rectangle.S0, t, rectangle.T1 - rectangle.T0, axis, length, origin);
    }

    /// <summary>Row-vector matrix scaling a unit primitive along a right-handed frame.</summary>
    private static Matrix4x4 Placement(D3 a, double sizeA, D3 b, double sizeB, D3 c, double sizeC, D3 origin)
    {
        if (a.Cross(b).Dot(c) < 0)
            b = -b;
        return new Matrix4x4(
            (float)(a.X * sizeA),
            (float)(a.Y * sizeA),
            (float)(a.Z * sizeA),
            0,
            (float)(b.X * sizeB),
            (float)(b.Y * sizeB),
            (float)(b.Z * sizeB),
            0,
            (float)(c.X * sizeC),
            (float)(c.Y * sizeC),
            (float)(c.Z * sizeC),
            0,
            (float)origin.X,
            (float)origin.Y,
            (float)origin.Z,
            1
        );
    }

    private static double SignedArea(V2[] loop)
    {
        double sum = 0;
        for (var index = 0; index < loop.Length; index++)
        {
            var a = loop[index];
            var b = loop[(index + 1) % loop.Length];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return sum / 2;
    }

    /// <summary>Removes vertices that lie on the line through their neighbours.</summary>
    private static V2[] Simplify(V2[] loop, double tolerance)
    {
        var points = loop.ToList();
        var changed = true;
        while (changed && points.Count > 3)
        {
            changed = false;
            for (var index = 0; index < points.Count && points.Count > 3; index++)
            {
                var a = points[(index + points.Count - 1) % points.Count];
                var b = points[index];
                var c = points[(index + 1) % points.Count];
                double dx = c.X - a.X,
                    dy = c.Y - a.Y;
                var span = Math.Sqrt(dx * dx + dy * dy);
                if (span < tolerance || Math.Abs(dx * (b.Y - a.Y) - dy * (b.X - a.X)) / span < tolerance)
                {
                    points.RemoveAt(index);
                    changed = true;
                    index--;
                }
            }
        }
        return points.ToArray();
    }

    private static bool TryRound(V2[] outline, double tolerance, out V2 centre, out double radius)
    {
        centre = default;
        radius = 0;
        if (outline.Length < 8)
            return false;
        var area = SignedArea(outline);
        double x = 0,
            y = 0;
        for (var index = 0; index < outline.Length; index++)
        {
            var a = outline[index];
            var b = outline[(index + 1) % outline.Length];
            var cross = a.X * b.Y - b.X * a.Y;
            x += (a.X + b.X) * cross;
            y += (a.Y + b.Y) * cross;
        }
        centre = new V2(x / (6 * area), y / (6 * area));
        var c = centre;
        var radii = outline.Select(point => Math.Sqrt((point.X - c.X) * (point.X - c.X) + (point.Y - c.Y) * (point.Y - c.Y))).ToArray();
        var mean = radii.Average();
        var deviation = Math.Sqrt(radii.Sum(value => (value - mean) * (value - mean)) / radii.Length);
        radius = radii.Max();
        return deviation <= Math.Max(tolerance, 0.01 * mean);
    }

    private static V2[] Rotate(V2[] loop, double angle)
    {
        double cos = Math.Cos(angle),
            sin = Math.Sin(angle);
        return loop.Select(point => new V2(point.X * cos + point.Y * sin, -point.X * sin + point.Y * cos)).ToArray();
    }

    /// <summary>Angle of the outline's longest edge, folded to [0, 90°).</summary>
    private static double MainAngle(V2[] outline)
    {
        var count = outline.Length;
        var longest = Enumerable
            .Range(0, count)
            .MaxBy(index => Distance(outline[index], outline[(index + 1) % count]));
        var direction = Subtract(outline[(longest + 1) % count], outline[longest]);
        var angle = Math.Atan2(direction.Y, direction.X);
        return angle - Math.Floor(angle / (Math.PI / 2)) * (Math.PI / 2);
    }

    /// <summary>
    /// Snaps a nearly rectilinear outline to its rectilinear skeleton: short edges off the two main
    /// directions (root fillets, chamfers) are dropped and their neighbours extended to meet.
    /// Long, slightly sloped edges (tapered flanges) count as aligned at their mid line.
    /// </summary>
    private static bool TrySnap(
        V2[] outline,
        double tolerance,
        PrimitiveFitOptions options,
        out V2[] corners,
        out double angle,
        out string failure
    )
    {
        corners = [];
        failure = "";
        var count = outline.Length;
        angle = MainAngle(outline);
        var points = Rotate(outline, angle);
        var size = Math.Max(
            points.Max(point => point.X) - points.Min(point => point.X),
            points.Max(point => point.Y) - points.Min(point => point.Y)
        );
        var taper = Math.Tan(options.MaxTaperAngle);

        // 0: off-axis, 1: along s (constant t), 2: along t (constant s).
        var kinds = new int[count];
        double perimeter = 0,
            offAxis = 0;
        for (var index = 0; index < count; index++)
        {
            var edge = Subtract(points[(index + 1) % count], points[index]);
            var length = Math.Sqrt(edge.X * edge.X + edge.Y * edge.Y);
            double dx = Math.Abs(edge.X),
                dy = Math.Abs(edge.Y);
            var tapered = length >= options.MinTaperLength * size;
            perimeter += length;
            kinds[index] =
                dy <= AngleTolerance * length || (tapered && dy <= taper * dx) ? 1
                : dx <= AngleTolerance * length || (tapered && dx <= taper * dy) ? 2
                : 0;
            if (kinds[index] == 0)
                offAxis += length;
        }
        if (offAxis > options.FilletShare * perimeter)
        {
            failure = "share";
            return false;
        }

        var first = Array.FindIndex(kinds, kind => kind != 0);
        var runs = new List<(int Kind, double Coordinate, int Direction)>();
        var gap = new List<V2>();
        for (var step = 0; step < count; step++)
        {
            var index = (first + step) % count;
            var a = points[index];
            var b = points[(index + 1) % count];
            if (kinds[index] == 0)
            {
                gap.Add(a);
                gap.Add(b);
                continue;
            }
            var coordinate = kinds[index] == 1 ? (a.Y + b.Y) / 2 : (a.X + b.X) / 2;
            var direction = Math.Sign(kinds[index] == 1 ? b.X - a.X : b.Y - a.Y);
            if (runs.Count > 0 && runs[^1].Kind == kinds[index])
            {
                // Collinear pieces of one edge merge.
                if (Math.Abs(runs[^1].Coordinate - coordinate) <= tolerance)
                {
                    gap.Clear();
                    continue;
                }
                if (!TryBridge(runs[^1], gap, out var bridge))
                {
                    failure = "parallel";
                    return false;
                }
                runs.Add(bridge);
            }
            runs.Add((kinds[index], coordinate, direction));
            gap.Clear();
        }
        if (runs.Count > 1 && runs[0].Kind == runs[^1].Kind)
        {
            if (Math.Abs(runs[0].Coordinate - runs[^1].Coordinate) <= tolerance)
                runs.RemoveAt(runs.Count - 1);
            else if (TryBridge(runs[^1], gap, out var bridge))
                runs.Add(bridge);
            else
            {
                failure = "parallel";
                return false;
            }
        }
        if (runs.Count < 4 || runs.Count % 2 != 0)
        {
            failure = "runs";
            return false;
        }

        var snapped = new V2[runs.Count];
        for (var index = 0; index < runs.Count; index++)
        {
            var (a, b) = (runs[index], runs[(index + 1) % runs.Count]);
            snapped[index] = a.Kind == 1 ? new V2(b.Coordinate, a.Coordinate) : new V2(a.Coordinate, b.Coordinate);
        }
        corners = Simplify(snapped, tolerance);
        if (corners.Length >= 4 && corners.Length % 2 == 0 && Math.Abs(SignedArea(corners)) > 0)
            return true;
        failure = "corners";
        return false;
    }

    /// <summary>
    /// Two parallel edges joined only by a curve (a rounded toe or end) get a square end at the
    /// curve's furthest point along the direction the outline was travelling.
    /// </summary>
    private static bool TryBridge(
        (int Kind, double Coordinate, int Direction) previous,
        List<V2> gap,
        out (int Kind, double Coordinate, int Direction) bridge
    )
    {
        bridge = default;
        if (gap.Count == 0 || previous.Direction == 0)
            return false;
        var along = gap.Select(point => previous.Kind == 1 ? point.X : point.Y).ToArray();
        bridge = (3 - previous.Kind, previous.Direction > 0 ? along.Max() : along.Min(), 0);
        return true;
    }

    private static V2 Subtract(V2 a, V2 b) => new(a.X - b.X, a.Y - b.Y);

    private static double Distance(V2 a, V2 b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static bool BoundsAgree(V2[] original, V2[] snapped, double tolerance)
    {
        var limit = Math.Max(
            2 * tolerance,
            0.02
                * Math.Max(
                    original.Max(point => point.X) - original.Min(point => point.X),
                    original.Max(point => point.Y) - original.Min(point => point.Y)
                )
        );
        return Math.Abs(original.Min(point => point.X) - snapped.Min(point => point.X)) <= limit
            && Math.Abs(original.Max(point => point.X) - snapped.Max(point => point.X)) <= limit
            && Math.Abs(original.Min(point => point.Y) - snapped.Min(point => point.Y)) <= limit
            && Math.Abs(original.Max(point => point.Y) - snapped.Max(point => point.Y)) <= limit;
    }

    /// <summary>Intersection over union of two outlines, integrated over horizontal scanlines.</summary>
    private static double IoU(V2[] a, V2[] b)
    {
        var low = Math.Min(a.Min(point => point.Y), b.Min(point => point.Y));
        var high = Math.Max(a.Max(point => point.Y), b.Max(point => point.Y));
        var step = (high - low) / IoURows;
        double intersection = 0,
            union = 0;
        for (var row = 0; row < IoURows; row++)
        {
            var y = low + (row + 0.5) * step;
            var first = Crossings(a, y, horizontal: true);
            var second = Crossings(b, y, horizontal: true);
            var overlap = Overlap(first, second);
            intersection += overlap;
            union += Length(first) + Length(second) - overlap;
        }
        return union > 0 ? intersection / union : 0;
    }

    /// <summary>Inside intervals of an outline along a line y = value (or x = value).</summary>
    private static List<(double Start, double End)> Crossings(V2[] loop, double value, bool horizontal)
    {
        var crossings = new List<double>();
        for (var index = 0; index < loop.Length; index++)
        {
            var p = loop[index];
            var q = loop[(index + 1) % loop.Length];
            var (pa, pb, qa, qb) = horizontal ? (p.Y, p.X, q.Y, q.X) : (p.X, p.Y, q.X, q.Y);
            if ((pa <= value) != (qa <= value))
                crossings.Add(pb + (value - pa) * (qb - pb) / (qa - pa));
        }
        crossings.Sort();
        var intervals = new List<(double, double)>(crossings.Count / 2);
        for (var index = 0; index + 1 < crossings.Count; index += 2)
            intervals.Add((crossings[index], crossings[index + 1]));
        return intervals;
    }

    private static double Length(List<(double Start, double End)> intervals) =>
        intervals.Sum(interval => interval.End - interval.Start);

    private static double Overlap(List<(double Start, double End)> first, List<(double Start, double End)> second)
    {
        double total = 0;
        int i = 0,
            j = 0;
        while (i < first.Count && j < second.Count)
        {
            var start = Math.Max(first[i].Start, second[j].Start);
            var end = Math.Min(first[i].End, second[j].End);
            if (end > start)
                total += end - start;
            if (first[i].End < second[j].End)
                i++;
            else
                j++;
        }
        return total;
    }

    /// <summary>
    /// Splits a rectilinear outline into rectangles by sweeping slabs between vertex coordinates and
    /// merging equal neighbouring intervals; both sweep directions are tried and the smaller kept.
    /// </summary>
    private static List<(double S0, double S1, double T0, double T1)> Partition(V2[] corners, double tolerance)
    {
        var alongS = Sweep(corners, tolerance, swap: false);
        var alongT = Sweep(corners, tolerance, swap: true);
        return alongT.Count < alongS.Count ? alongT : alongS;
    }

    private static List<(double S0, double S1, double T0, double T1)> Sweep(V2[] corners, double tolerance, bool swap)
    {
        var points = swap ? corners.Select(point => new V2(point.Y, point.X)).ToArray() : corners;
        var stops = new List<double>();
        foreach (var x in points.Select(point => point.X).OrderBy(x => x))
        {
            if (stops.Count == 0 || x - stops[^1] > tolerance)
                stops.Add(x);
        }

        var result = new List<(double, double, double, double)>();
        var open = new List<(double Start, double Low, double High)>();
        for (var slab = 0; slab + 1 < stops.Count; slab++)
        {
            var intervals = Crossings(points, (stops[slab] + stops[slab + 1]) / 2, horizontal: false);
            var next = new List<(double Start, double Low, double High)>();
            foreach (var (low, high) in intervals)
            {
                var index = open.FindIndex(entry =>
                    Math.Abs(entry.Low - low) <= tolerance && Math.Abs(entry.High - high) <= tolerance
                );
                if (index >= 0)
                {
                    next.Add(open[index]);
                    open.RemoveAt(index);
                }
                else
                    next.Add((stops[slab], low, high));
            }
            foreach (var entry in open)
                result.Add((entry.Start, stops[slab], entry.Low, entry.High));
            open = next;
        }
        foreach (var entry in open)
            result.Add((entry.Start, stops[^1], entry.Low, entry.High));
        return swap
            ? result.Select(rectangle => (rectangle.Item3, rectangle.Item4, rectangle.Item1, rectangle.Item2)).ToList()
            : result;
    }

    /// <summary>One box when the closed mesh fills nearly all of its oriented bounding box.</summary>
    private static PrimitiveOutcome CubeLike(
        RvmMesh? mesh,
        List<(D3 Direction, double Area)> directions,
        PrimitiveFitOptions options,
        double tolerance
    )
    {
        if (mesh == null)
            return PrimitiveOutcome.Kept("no-mesh");
        var welder = new Welder(tolerance);
        var ids = mesh.Vertices.Select(vertex => welder.Add(D3.From(vertex))).ToArray();
        if (welder.Count < options.CubeMinVertices || welder.Count > options.CubeMaxVertices)
            return PrimitiveOutcome.Kept("cube-vertices");

        var area = new D3(0, 0, 0);
        double totalArea = 0;
        double volume = 0;
        for (var index = 0; index + 2 < mesh.Triangles.Length; index += 3)
        {
            var a = ids[mesh.Triangles[index]];
            var b = ids[mesh.Triangles[index + 1]];
            var c = ids[mesh.Triangles[index + 2]];
            if (a == b || b == c || c == a)
                continue;
            var (pa, pb, pc) = (welder.Points[a], welder.Points[b], welder.Points[c]);
            var normal = (pb - pa).Cross(pc - pa);
            area += normal;
            totalArea += normal.Length;
            volume += pa.Dot(pb.Cross(pc)) / 6;
        }
        // A closed surface has no net area vector; unlike edge pairing, this tolerates the
        // T-junctions tessellated caps leave at collinear vertices.
        if (!(totalArea > 0) || area.Length > 1e-4 * totalArea)
            return PrimitiveOutcome.Kept("cube-open");
        volume = Math.Abs(volume);

        var ranked = directions.Take(12).Select(entry => entry.Direction).ToArray();
        (D3 A, D3 B, D3 C, D3 Low, D3 High, double Volume)? best = null;
        for (var first = 0; first < ranked.Length; first++)
        for (var second = first + 1; second < ranked.Length; second++)
        {
            if (Math.Abs(ranked[first].Dot(ranked[second])) > PerpendicularDot)
                continue;
            var a = ranked[first];
            var b = (ranked[second] - a * a.Dot(ranked[second])).Normalized();
            var c = a.Cross(b);
            var low = new D3(double.MaxValue, double.MaxValue, double.MaxValue);
            var high = new D3(double.MinValue, double.MinValue, double.MinValue);
            foreach (var point in welder.Points)
            {
                var local = new D3(point.Dot(a), point.Dot(b), point.Dot(c));
                low = new D3(Math.Min(low.X, local.X), Math.Min(low.Y, local.Y), Math.Min(low.Z, local.Z));
                high = new D3(Math.Max(high.X, local.X), Math.Max(high.Y, local.Y), Math.Max(high.Z, local.Z));
            }
            var size = high - low;
            var boxVolume = size.X * size.Y * size.Z;
            if (best == null || boxVolume < best.Value.Volume)
                best = (a, b, c, low, high, boxVolume);
        }
        if (best is not { } frame || !(frame.Volume > 0))
            return PrimitiveOutcome.Kept("cube-frame");

        var dimensions = frame.High - frame.Low;
        var missing = 1 - volume / frame.Volume;
        var small =
            dimensions.X < options.CubeSmallMaxDimension
            && dimensions.Y < options.CubeSmallMaxDimension
            && dimensions.Z < options.CubeSmallMaxDimension;
        var allowed = Math.Max(
            small ? options.CubeSmallMaxMissingFraction : options.CubeMaxMissingFraction,
            options.NearBoxMaxMissingFraction
        );
        if (missing < -1e-6 || missing > allowed)
            return PrimitiveOutcome.Kept("cube-fill");

        var middle = (frame.Low + frame.High) / 2;
        var origin = frame.A * middle.X + frame.B * middle.Y + frame.C * middle.Z;
        return PrimitiveOutcome.Replaced(
            new PrimitiveFit(
                "CUBELIKE",
                [
                    new PrimitivePart(
                        PrimitivePartKind.Cube,
                        Placement(frame.A, dimensions.X, frame.B, dimensions.Y, frame.C, dimensions.Z, origin)
                    ),
                ],
                Math.Max(missing, 0),
                1 - Math.Max(missing, 0)
            )
        );
    }

    /// <summary>Merges points closer than the tolerance, using a hash grid.</summary>
    private sealed class Welder(double tolerance)
    {
        private readonly Dictionary<(long, long, long), List<int>> _cells = new();

        internal List<D3> Points { get; } = new();

        internal int Count => Points.Count;

        internal int Add(D3 point)
        {
            var x = (long)Math.Floor(point.X / tolerance);
            var y = (long)Math.Floor(point.Y / tolerance);
            var z = (long)Math.Floor(point.Z / tolerance);
            var squared = tolerance * tolerance;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (!_cells.TryGetValue((x + dx, y + dy, z + dz), out var cell))
                    continue;
                foreach (var id in cell)
                {
                    var offset = Points[id] - point;
                    if (offset.Dot(offset) <= squared)
                        return id;
                }
            }
            var added = Points.Count;
            Points.Add(point);
            if (!_cells.TryGetValue((x, y, z), out var list))
                _cells[(x, y, z)] = list = new List<int>(1);
            list.Add(added);
            return added;
        }
    }
}
