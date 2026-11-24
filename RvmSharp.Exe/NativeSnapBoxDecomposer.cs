namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Linq;
using Tessellation;

/// <summary>
/// Snap-box decomposition, the last fit tried before a template is kept. Hand-modelled panels and
/// sections are often almost a union of boxes: a face tilted a fraction of a degree, rounded or
/// sniped corners, chamfered roots, small bolt holes. Faces close to three perpendicular directions
/// are snapped to grid planes, minor faces are ignored, cells inside the real mesh are merged into
/// boxes, and the result must stay within a gap that grows with the template's size.
/// </summary>
internal static partial class NativePrimitiveFitter
{
    private const int SnapBoxMaxTriangles = 1024;
    private const int SnapBoxMaxCells = 4096;
    private const int SnapBoxFrameDirections = 6;

    /// <summary>Smallest share of the surface area that must lie within the snap angle of the frame.</summary>
    private const double SnapBoxMinAlignedShare = 0.65;

    /// <summary>Largest distance, in metres, between face positions merged into one plane.</summary>
    private const double SnapBoxPlaneTolerance = 1e-3;

    /// <summary>Most gap-check samples per check; larger parts are sampled more sparsely.</summary>
    private const double SnapBoxGapSamples = 5_000;

    private static bool TrySnapBoxes(
        List<Face> faces,
        List<(D3 Direction, double Area)> directions,
        RvmMesh? mesh,
        PrimitiveFitOptions options,
        out PrimitiveFit fit,
        out string failure
    )
    {
        fit = null!;
        if (mesh == null || mesh.Triangles.Length < 12 || mesh.Triangles.Length / 3 > SnapBoxMaxTriangles)
        {
            failure = "size";
            return false;
        }
        var triangles = new (D3 A, D3 B, D3 C)[mesh.Triangles.Length / 3];
        var net = new D3(0, 0, 0);
        double totalArea = 0,
            meshVolume = 0;
        for (var index = 0; index < triangles.Length; index++)
        {
            var (a, b, c) = (
                D3.From(mesh.Vertices[mesh.Triangles[index * 3]]),
                D3.From(mesh.Vertices[mesh.Triangles[index * 3 + 1]]),
                D3.From(mesh.Vertices[mesh.Triangles[index * 3 + 2]])
            );
            triangles[index] = (a, b, c);
            var normal = (b - a).Cross(c - a);
            net += normal;
            totalArea += normal.Length;
            meshVolume += a.Dot(b.Cross(c)) / 6;
        }
        meshVolume = Math.Abs(meshVolume);
        if (!(totalArea > 0) || net.Length > 1e-4 * totalArea || !(meshVolume > 0))
        {
            failure = "open";
            return false;
        }

        if (!TrySnapFrame(faces, directions, options, out var axes, out var alignedAxis))
        {
            failure = "aligned";
            return false;
        }

        var low = new double[3];
        var high = new double[3];
        var points = faces.SelectMany(face => face.Contours.SelectMany(contour => contour)).ToArray();
        for (var axis = 0; axis < 3; axis++)
        {
            low[axis] = points.Min(p => p.Dot(axes[axis]));
            high[axis] = points.Max(p => p.Dot(axes[axis]));
            if (!(high[axis] - low[axis] > 0))
            {
                failure = "aligned";
                return false;
            }
        }
        var extents = new[] { high[0] - low[0], high[1] - low[1], high[2] - low[2] };
        var largest = extents.Max();
        var gap = Math.Max(options.MaxGap, Math.Min(options.SnapBoxGapShare * largest, options.SnapBoxMaxGap));
        var missingGap = Math.Max(options.MaxMissingGap, gap);

        // Coarse planes first, for the fewest boxes; then with every face wider than MaxGap, for
        // thin legs and flanges that the coarse grid fills over.
        double[][]? tried = null;
        failure = "";
        foreach (var minimumWidth in new[] { gap, options.MaxGap })
        {
            var coordinates = new double[3][];
            for (var axis = 0; axis < 3; axis++)
                coordinates[axis] = SnapPlanes(faces, axes, alignedAxis, axis, low, high, extents, minimumWidth);
            if (tried != null && coordinates.Zip(tried).All(pair => pair.First.SequenceEqual(pair.Second)))
                break;
            tried = coordinates;
            if (TrySnapGrid(triangles, axes, coordinates, meshVolume, gap, missingGap, options, out fit, out failure))
                return true;
        }
        return false;
    }

    private static bool TrySnapGrid(
        (D3 A, D3 B, D3 C)[] triangles,
        D3[] axes,
        double[][] coordinates,
        double meshVolume,
        double gap,
        double missingGap,
        PrimitiveFitOptions options,
        out PrimitiveFit fit,
        out string failure
    )
    {
        fit = null!;
        var size = new[] { coordinates[0].Length - 1, coordinates[1].Length - 1, coordinates[2].Length - 1 };
        if ((long)size[0] * size[1] * size[2] > SnapBoxMaxCells)
        {
            failure = "grid";
            return false;
        }

        var inside = SnapOccupancy(triangles, axes, coordinates, size);
        if (inside == null)
        {
            failure = "occupancy";
            return false;
        }
        var best = inside.Any(cell => cell) ? MergeBest(inside, size) : null;
        if (best == null || best.Count > options.SnapBoxMaxParts)
        {
            failure = "parts";
            return false;
        }

        double boxVolume = 0;
        foreach (var cuboid in best)
        {
            double volume = 1;
            for (var axis = 0; axis < 3; axis++)
                volume *= coordinates[axis][cuboid.High[axis]] - coordinates[axis][cuboid.Low[axis]];
            boxVolume += volume;
        }
        var volumeError = Math.Abs(boxVolume - meshVolume) / meshVolume;
        if (volumeError > options.SnapBoxMaxVolumeError)
        {
            failure = "volume";
            return false;
        }

        var parts = CubeParts(best, axes, coordinates);
        if (!SnapWithinGap(parts, triangles, gap, missingGap, out failure))
            return false;
        fit = new PrimitiveFit("SNAPBOXES_" + parts.Length, parts, volumeError, 1 - volumeError);
        return true;
    }

    /// <summary>
    /// The frame, from pairs of the largest face directions that are nearly perpendicular, under
    /// which the most surface lies within the snap angle; and the frame axis of every aligned face
    /// (-1 for minor faces).
    /// </summary>
    private static bool TrySnapFrame(
        List<Face> faces,
        List<(D3 Direction, double Area)> directions,
        PrimitiveFitOptions options,
        out D3[] axes,
        out int[] alignedAxis
    )
    {
        axes = null!;
        alignedAxis = null!;
        var angle = options.SnapBoxMaxAngle * Math.PI / 180;
        var perpendicular = Math.Sin(angle);
        var aligned = Math.Cos(angle);
        var total = faces.Sum(face => face.Area);
        var points = faces.SelectMany(face => face.Contours.SelectMany(contour => contour)).ToArray();
        var ranked = directions.Take(SnapBoxFrameDirections).Select(entry => entry.Direction).ToArray();
        double bestArea = 0,
            bestVolume = double.MaxValue;
        for (var first = 0; first < ranked.Length; first++)
        for (var second = first + 1; second < ranked.Length; second++)
        {
            if (Math.Abs(ranked[first].Dot(ranked[second])) > perpendicular)
                continue;
            var a = ranked[first];
            var b = (ranked[second] - a * a.Dot(ranked[second])).Normalized();
            var frame = new[] { a, b, a.Cross(b) };
            double area = 0;
            foreach (var face in faces)
            {
                if (frame.Any(axis => Math.Abs(face.Normal.Dot(axis)) >= aligned))
                    area += face.Area;
            }
            if (area < bestArea * (1 - 1e-9))
                continue;
            // Frames that snap the same faces differ by a fraction of a degree; the tightest one
            // follows the untilted faces.
            double volume = 1;
            foreach (var axis in frame)
                volume *= points.Max(p => p.Dot(axis)) - points.Min(p => p.Dot(axis));
            if (area > bestArea * (1 + 1e-9) || volume < bestVolume)
                (bestArea, bestVolume, axes) = (Math.Max(area, bestArea), volume, frame);
        }
        if (axes == null || bestArea < SnapBoxMinAlignedShare * total)
            return false;
        alignedAxis = new int[faces.Count];
        for (var index = 0; index < faces.Count; index++)
        {
            alignedAxis[index] = -1;
            for (var axis = 0; axis < 3; axis++)
            {
                if (Math.Abs(faces[index].Normal.Dot(axes[axis])) >= aligned)
                    alignedAxis[index] = axis;
            }
        }
        return true;
    }

    /// <summary>
    /// Grid coordinates along one axis: the bounds, and the positions of aligned faces (a tilted
    /// face at the middle of its range). A face narrower than <paramref name="minimumWidth"/> (the flats
    /// of a bolt hole, the tip of a snipe) adds no plane; it is squared off or filled over.
    /// </summary>
    private static double[] SnapPlanes(
        List<Face> faces,
        D3[] axes,
        int[] alignedAxis,
        int axis,
        double[] low,
        double[] high,
        double[] extents,
        double minimumWidth
    )
    {
        var (first, second) = ((axis + 1) % 3, (axis + 2) % 3);
        // Coplanar polygons are one face. A tilted face snaps to the middle of its range, which
        // keeps the largest deviation from it smallest.
        var planar = new List<(D3 Normal, double Offset, double Area, List<D3> Points)>();
        for (var index = 0; index < faces.Count; index++)
        {
            if (alignedAxis[index] != axis)
                continue;
            var face = faces[index];
            var normal = face.Normal.Dot(axes[axis]) < 0 ? -face.Normal : face.Normal;
            var offset = Centroid(face).Dot(normal);
            var group = planar.FindIndex(entry =>
                entry.Normal.Dot(normal) > SameDirectionDot && Math.Abs(entry.Offset - offset) <= SnapBoxPlaneTolerance
            );
            var points = face.Contours.SelectMany(contour => contour);
            if (group < 0)
                planar.Add((normal, offset, face.Area, points.ToList()));
            else
            {
                var entry = planar[group];
                entry.Points.AddRange(points);
                planar[group] = (entry.Normal, entry.Offset, entry.Area + face.Area, entry.Points);
            }
        }
        double Range(List<D3> points, D3 direction, out double middle)
        {
            var (minimum, maximum) = (points.Min(p => p.Dot(direction)), points.Max(p => p.Dot(direction)));
            middle = (minimum + maximum) / 2;
            return maximum - minimum;
        }
        var positions = planar
            .Select(entry =>
            {
                Range(entry.Points, axes[axis], out var middle);
                var width = Math.Min(Range(entry.Points, axes[first], out _), Range(entry.Points, axes[second], out _));
                return (Position: middle, entry.Area, Width: width);
            })
            .ToList();
        positions.Sort((x, y) => x.Position.CompareTo(y.Position));

        var snap = Math.Min(SnapBoxPlaneTolerance, 0.1 * extents.Min());
        var planes = new List<double> { low[axis] };
        var next = 0;
        while (next < positions.Count)
        {
            double weighted = 0,
                area = 0,
                width = 0;
            var start = positions[next].Position;
            while (next < positions.Count && positions[next].Position - start <= snap)
            {
                weighted += positions[next].Position * positions[next].Area;
                area += positions[next].Area;
                width = Math.Max(width, positions[next].Width);
                next++;
            }
            var position = weighted / area;
            if (width < minimumWidth || position - low[axis] <= snap || high[axis] - position <= snap)
                continue;
            if (position - planes[^1] > snap)
                planes.Add(position);
        }
        planes.Add(high[axis]);
        return planes.ToArray();
    }

    /// <summary>Area centroid of a face; hole contours wind the other way and subtract.</summary>
    private static D3 Centroid(Face face)
    {
        var sum = new D3(0, 0, 0);
        double area = 0;
        foreach (var contour in face.Contours)
        {
            for (var index = 2; index < contour.Length; index++)
            {
                var (a, b, c) = (contour[0], contour[index - 1], contour[index]);
                var signed = (b - a).Cross(c - a).Dot(face.Normal) / 2;
                sum += (a + b + c) * (signed / 3);
                area += signed;
            }
        }
        if (Math.Abs(area) > 1e-6 * face.Area)
            return sum / area;
        var points = face.Contours.SelectMany(contour => contour).ToArray();
        return points.Aggregate(new D3(0, 0, 0), (total, point) => total + point) / points.Length;
    }

    /// <summary>
    /// Inside/outside of every cell centre by ray parity against the mesh, one ray per column along
    /// the first axis. Rays are nudged off the column centre so they miss mesh edges; null when a
    /// ray crosses the surface an odd number of times.
    /// </summary>
    private static bool[]? SnapOccupancy((D3 A, D3 B, D3 C)[] triangles, D3[] axes, double[][] coordinates, int[] size)
    {
        var local = triangles
            .Select(triangle =>
            {
                D3 Local(D3 p) => new(p.Dot(axes[0]), p.Dot(axes[1]), p.Dot(axes[2]));
                return (A: Local(triangle.A), B: Local(triangle.B), C: Local(triangle.C));
            })
            .ToArray();
        var inside = new bool[size[0] * size[1] * size[2]];
        var crossings = new List<double>();
        for (var j = 0; j < size[1]; j++)
        for (var k = 0; k < size[2]; k++)
        {
            var py = coordinates[1][j] + (coordinates[1][j + 1] - coordinates[1][j]) * 0.5123;
            var pz = coordinates[2][k] + (coordinates[2][k + 1] - coordinates[2][k]) * 0.4871;
            crossings.Clear();
            foreach (var (a, b, c) in local)
            {
                var d = (b.Y - a.Y) * (c.Z - a.Z) - (c.Y - a.Y) * (b.Z - a.Z);
                if (Math.Abs(d) < 1e-18)
                    continue;
                var u = ((py - a.Y) * (c.Z - a.Z) - (c.Y - a.Y) * (pz - a.Z)) / d;
                var v = ((b.Y - a.Y) * (pz - a.Z) - (py - a.Y) * (b.Z - a.Z)) / d;
                if (u < 0 || v < 0 || u + v > 1)
                    continue;
                crossings.Add(a.X + (b.X - a.X) * u + (c.X - a.X) * v);
            }
            if (crossings.Count % 2 != 0)
                return null;
            for (var i = 0; i < size[0]; i++)
            {
                var centre = (coordinates[0][i] + coordinates[0][i + 1]) / 2;
                inside[Index(size, i, j, k)] = crossings.Count(x => x > centre) % 2 == 1;
            }
        }
        return inside;
    }

    /// <summary>
    /// Like <see cref="WithinGap"/>, with the snap-box limits: box surface outside the source solid
    /// stays within <paramref name="gap"/> of the source surface, and every point of the source
    /// surface lies within <paramref name="missingGap"/> of a box face.
    /// </summary>
    private static bool SnapWithinGap(
        PrimitivePart[] parts,
        (D3 A, D3 B, D3 C)[] triangles,
        double gap,
        double missingGap,
        out string failure
    )
    {
        failure = "gap";
        double partArea = 0;
        foreach (var part in parts)
        {
            var m = part.Matrix;
            var x = new D3(m.M11, m.M12, m.M13).Length;
            var y = new D3(m.M21, m.M22, m.M23).Length;
            var z = new D3(m.M31, m.M32, m.M33).Length;
            partArea += 2 * (x * y + y * z + z * x);
        }
        var spacing = Math.Max(gap, Math.Sqrt(partArea / SnapBoxGapSamples));
        var frames = parts.Select(BoxFrame.From).ToArray();
        // Neighbouring samples are usually nearest the same triangle, so that one is tried first.
        var last = 0;
        bool Near(D3 point, double distance)
        {
            var squared = distance * distance;
            for (var step = 0; step <= triangles.Length; step++)
            {
                var index = step == 0 ? last : step - 1;
                var (a, b, c) = triangles[index];
                var offset = point - ClosestPoint(point, a, b, c);
                if (offset.Dot(offset) <= squared)
                {
                    last = index;
                    return true;
                }
            }
            return false;
        }
        // Faces shared with another part are inside the replacement, not on its surface.
        bool Hidden(D3 point, BoxFrame owner) =>
            frames.Any(frame => frame.Part != owner.Part && frame.Contains(point))
            || Math.Abs(Winding(point, triangles)) >= 0.5;

        foreach (var frame in frames)
        {
            var axes = new[] { frame.X, frame.Y, frame.Z };
            var halves = new[] { frame.HalfX, frame.HalfY, frame.HalfZ };
            for (var normal = 0; normal < 3; normal++)
            {
                var (u, v) = ((normal + 1) % 3, (normal + 2) % 3);
                var countU = Math.Clamp((int)Math.Ceiling(2 * halves[u] / spacing) + 1, 2, 1025);
                var countV = Math.Clamp((int)Math.Ceiling(2 * halves[v] / spacing) + 1, 2, 1025);
                var stepU = 2 * halves[u] / (countU - 1);
                var stepV = 2 * halves[v] / (countV - 1);
                // The distance to the source surface changes no faster than the position, so a
                // sample that clears the gap by its cell's radius clears its whole cell.
                var radius = Math.Sqrt(stepU * stepU + stepV * stepV) / 2;
                D3 At(int side, double a, double b) =>
                    frame.Origin
                    + axes[normal] * (side * halves[normal])
                    + axes[u] * Math.Clamp(a, -halves[u], halves[u])
                    + axes[v] * Math.Clamp(b, -halves[v], halves[v]);
                for (var side = -1; side <= 1; side += 2)
                for (var i = 0; i < countU; i++)
                for (var j = 0; j < countV; j++)
                {
                    var (a, b) = (-halves[u] + i * stepU, -halves[v] + j * stepV);
                    var point = At(side, a, b);
                    if ((radius < gap && Near(point, gap - radius)) || (radius <= gap && Hidden(point, frame)))
                        continue;
                    // Close to the limit, or far enough off that the cell needs a closer look.
                    for (var di = -4; di <= 4; di++)
                    for (var dj = -4; dj <= 4; dj++)
                    {
                        var fine = At(side, a + di * stepU / 8, b + dj * stepV / 8);
                        if (!Near(fine, gap) && !Hidden(fine, frame))
                            return false;
                    }
                }
            }
        }

        failure = "missing";
        bool Covered(D3 point)
        {
            foreach (var frame in frames)
            {
                if (frame.Distance(point) <= missingGap)
                    return true;
            }
            return false;
        }
        var sourceArea = triangles.Sum(t => (t.B - t.A).Cross(t.C - t.A).Length / 2);
        var sourceSpacing = Math.Max(missingGap / 2, Math.Sqrt(sourceArea / SnapBoxGapSamples));
        foreach (var (a, b, c) in triangles)
        {
            // Corners, points along the edges, and interior points in proportion to the area, so
            // long slivers cost no more than their size.
            if (!Covered(a))
                return false;
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, a) })
            {
                var steps = Math.Clamp((int)Math.Ceiling((to - from).Length / sourceSpacing), 1, 64);
                for (var step = 1; step < steps; step++)
                {
                    if (!Covered(from + (to - from) * ((double)step / steps)))
                        return false;
                }
            }
            var area = (b - a).Cross(c - a).Length / 2;
            var interior = Math.Min((int)(area / (sourceSpacing * sourceSpacing)), 256);
            for (var sample = 1; sample <= interior; sample++)
            {
                // Additive recurrence (R2 sequence), folded into the triangle.
                var u = sample * 0.7548776662466927 % 1;
                var v = sample * 0.5698402909980532 % 1;
                if (u + v > 1)
                    (u, v) = (1 - u, 1 - v);
                if (!Covered(a + (b - a) * u + (c - a) * v))
                    return false;
            }
        }
        failure = "";
        return true;
    }

    /// <summary>A box part as its centre, unit axes and half sizes.</summary>
    private readonly record struct BoxFrame(
        PrimitivePart Part,
        D3 Origin,
        D3 X,
        D3 Y,
        D3 Z,
        double HalfX,
        double HalfY,
        double HalfZ
    )
    {
        internal static BoxFrame From(PrimitivePart part)
        {
            var m = part.Matrix;
            var x = new D3(m.M11, m.M12, m.M13);
            var y = new D3(m.M21, m.M22, m.M23);
            var z = new D3(m.M31, m.M32, m.M33);
            return new BoxFrame(
                part,
                new D3(m.M41, m.M42, m.M43),
                x.Normalized(),
                y.Normalized(),
                z.Normalized(),
                x.Length / 2,
                y.Length / 2,
                z.Length / 2
            );
        }

        /// <summary>Whether the point is inside the box or on its surface, up to float rounding.</summary>
        internal bool Contains(D3 point)
        {
            var offset = point - Origin;
            var slack = 1e-6 * (HalfX + HalfY + HalfZ);
            return Math.Abs(offset.Dot(X)) <= HalfX + slack
                && Math.Abs(offset.Dot(Y)) <= HalfY + slack
                && Math.Abs(offset.Dot(Z)) <= HalfZ + slack;
        }

        /// <summary>Distance from a point to the box surface, from inside or outside.</summary>
        internal double Distance(D3 point)
        {
            var offset = point - Origin;
            var x = Math.Abs(offset.Dot(X)) - HalfX;
            var y = Math.Abs(offset.Dot(Y)) - HalfY;
            var z = Math.Abs(offset.Dot(Z)) - HalfZ;
            if (x <= 0 && y <= 0 && z <= 0)
                return -Math.Max(x, Math.Max(y, z));
            (x, y, z) = (Math.Max(x, 0), Math.Max(y, 0), Math.Max(z, 0));
            return Math.Sqrt(x * x + y * y + z * z);
        }
    }
}
