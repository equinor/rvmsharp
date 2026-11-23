namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Linq;
using Tessellation;

/// <summary>
/// Exact box decomposition. A facet group whose faces all lie in three perpendicular directions
/// (L-panels, notched or holed plates, stacked blocks, channels) is a union of boxes on the grid of
/// its own vertex coordinates. The cells inside it are found by ray parity and merged greedily into
/// as few boxes as possible, so the replacement has exactly the source's shape.
/// </summary>
internal static partial class NativePrimitiveFitter
{
    private const int BoxMaxCells = 100_000;
    private const int BoxMaxFaces = 4096;
    private const double BoxVolumeTolerance = 1e-4;

    private readonly record struct Cuboid(int[] Low, int[] High);

    private static bool TryBoxes(
        List<Face> faces,
        List<(D3 Direction, double Area)> directions,
        RvmMesh? mesh,
        int maxParts,
        out PrimitiveFit fit
    )
    {
        fit = null!;
        if (directions.Count != 3 || faces.Count > BoxMaxFaces || mesh == null)
            return false;
        var axes = directions.Select(entry => entry.Direction).ToArray();
        if (
            Math.Abs(axes[0].Dot(axes[1])) > PerpendicularDot
            || Math.Abs(axes[0].Dot(axes[2])) > PerpendicularDot
            || Math.Abs(axes[1].Dot(axes[2])) > PerpendicularDot
        )
            return false;

        var points = faces.SelectMany(face => face.Contours.SelectMany(contour => contour)).ToArray();
        var extent = 0.0;
        for (var axis = 0; axis < 3; axis++)
            extent = Math.Max(extent, points.Max(p => p.Dot(axes[axis])) - points.Min(p => p.Dot(axes[axis])));
        var tolerance = Math.Max(1e-7, extent * 1e-5);
        var coordinates = new double[3][];
        for (var axis = 0; axis < 3; axis++)
        {
            var values = points.Select(p => p.Dot(axes[axis])).OrderBy(value => value).ToArray();
            var distinct = new List<double> { values[0] };
            foreach (var value in values)
            {
                if (value - distinct[^1] > tolerance)
                    distinct.Add(value);
            }
            if (distinct.Count < 2)
                return false;
            coordinates[axis] = distinct.ToArray();
        }
        var size = new[] { coordinates[0].Length - 1, coordinates[1].Length - 1, coordinates[2].Length - 1 };
        if ((long)size[0] * size[1] * size[2] > BoxMaxCells)
            return false;

        // Two independent ray directions must agree; an open or self-overlapping shell would not.
        var inside = Occupancy(faces, axes, coordinates, size, 0, tolerance);
        if (inside == null || !inside.SequenceEqual(Occupancy(faces, axes, coordinates, size, 1, tolerance) ?? []))
            return false;

        double cellVolume = 0;
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
        for (var k = 0; k < size[2]; k++)
        {
            if (inside[Index(size, i, j, k)])
                cellVolume +=
                    (coordinates[0][i + 1] - coordinates[0][i])
                    * (coordinates[1][j + 1] - coordinates[1][j])
                    * (coordinates[2][k + 1] - coordinates[2][k]);
        }
        var meshVolume = Math.Abs(MeshVolume(mesh));
        if (!(cellVolume > 0) || Math.Abs(cellVolume - meshVolume) > BoxVolumeTolerance * meshVolume)
            return false;

        // Greedy merging depends on axis order and scan direction, so try every order, each
        // mirrored along every combination of axes (only the plain orders on very large grids).
        List<Cuboid>? best = null;
        var mirrors = (long)size[0] * size[1] * size[2] <= BoxMaxCells / 8 ? 8 : 1;
        for (var mirror = 0; mirror < mirrors; mirror++)
        {
            var flipped = mirror == 0 ? inside : Mirror(inside, size, mirror);
            foreach (var order in AxisOrders)
            {
                var cuboids = Merge(flipped, size, order);
                if (best != null && cuboids.Count >= best.Count)
                    continue;
                best = mirror == 0 ? cuboids : cuboids.Select(cuboid => Unmirror(cuboid, size, mirror)).ToList();
            }
        }
        if (best == null || best.Count > maxParts)
            return false;

        var parts = best.Select(cuboid =>
            {
                var low = new double[3];
                var high = new double[3];
                for (var axis = 0; axis < 3; axis++)
                {
                    low[axis] = coordinates[axis][cuboid.Low[axis]];
                    high[axis] = coordinates[axis][cuboid.High[axis]];
                }
                var origin =
                    axes[0] * ((low[0] + high[0]) / 2) + axes[1] * ((low[1] + high[1]) / 2) + axes[2] * ((low[2] + high[2]) / 2);
                return new PrimitivePart(
                    PrimitivePartKind.Cube,
                    Placement(axes[0], high[0] - low[0], axes[1], high[1] - low[1], axes[2], high[2] - low[2], origin)
                );
            })
            .ToArray();
        fit = new PrimitiveFit("BOXES_" + parts.Length, parts, Math.Abs(cellVolume - meshVolume) / meshVolume, 1);
        return true;
    }

    private static readonly int[][] AxisOrders =
    [
        [0, 1, 2],
        [0, 2, 1],
        [1, 0, 2],
        [1, 2, 0],
        [2, 0, 1],
        [2, 1, 0],
    ];

    private static int Index(int[] size, int i, int j, int k) => (i * size[1] + j) * size[2] + k;

    /// <summary>The grid reversed along every axis whose bit is set in <paramref name="mask"/>.</summary>
    private static bool[] Mirror(bool[] inside, int[] size, int mask)
    {
        var result = new bool[inside.Length];
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
        for (var k = 0; k < size[2]; k++)
        {
            var mi = (mask & 1) != 0 ? size[0] - 1 - i : i;
            var mj = (mask & 2) != 0 ? size[1] - 1 - j : j;
            var mk = (mask & 4) != 0 ? size[2] - 1 - k : k;
            result[Index(size, mi, mj, mk)] = inside[Index(size, i, j, k)];
        }
        return result;
    }

    /// <summary>A box found on a mirrored grid, in the original grid's indices.</summary>
    private static Cuboid Unmirror(Cuboid cuboid, int[] size, int mask)
    {
        var low = (int[])cuboid.Low.Clone();
        var high = (int[])cuboid.High.Clone();
        for (var axis = 0; axis < 3; axis++)
        {
            if ((mask & (1 << axis)) == 0)
                continue;
            (low[axis], high[axis]) = (size[axis] - cuboid.High[axis], size[axis] - cuboid.Low[axis]);
        }
        return new Cuboid(low, high);
    }

    /// <summary>
    /// Inside/outside of every grid cell from rays along <paramref name="rayAxis"/>: a cell is inside
    /// when the faces square to that axis are crossed an odd number of times beyond its centre. Cell
    /// centres never lie on a face edge, because every edge lies on a grid line.
    /// </summary>
    private static bool[]? Occupancy(
        List<Face> faces,
        D3[] axes,
        double[][] coordinates,
        int[] size,
        int rayAxis,
        double tolerance
    )
    {
        var (u, v) = ((rayAxis + 1) % 3, (rayAxis + 2) % 3);
        var caps = new List<(double Position, (double U, double V)[][] Contours)>();
        foreach (var face in faces)
        {
            if (Math.Abs(face.Normal.Dot(axes[rayAxis])) < 0.5)
                continue;
            caps.Add((
                face.Contours.SelectMany(c => c).Average(p => p.Dot(axes[rayAxis])),
                face.Contours.Select(contour => contour.Select(p => (p.Dot(axes[u]), p.Dot(axes[v]))).ToArray()).ToArray()
            ));
        }
        if (caps.Count < 2)
            return null;

        var inside = new bool[size[0] * size[1] * size[2]];
        var cell = new int[3];
        var crossings = new List<double>();
        for (var a = 0; a < size[u]; a++)
        for (var b = 0; b < size[v]; b++)
        {
            var pu = (coordinates[u][a] + coordinates[u][a + 1]) / 2;
            var pv = (coordinates[v][b] + coordinates[v][b + 1]) / 2;
            crossings.Clear();
            foreach (var (position, contours) in caps)
            {
                if (ContainsEvenOdd(contours, pu, pv) && !crossings.Any(x => Math.Abs(x - position) <= tolerance))
                    crossings.Add(position);
            }
            if (crossings.Count % 2 != 0)
                return null;
            for (var c = 0; c < size[rayAxis]; c++)
            {
                var centre = (coordinates[rayAxis][c] + coordinates[rayAxis][c + 1]) / 2;
                var beyond = crossings.Count(x => x > centre);
                cell[rayAxis] = c;
                cell[u] = a;
                cell[v] = b;
                inside[Index(size, cell[0], cell[1], cell[2])] = beyond % 2 == 1;
            }
        }
        return inside;
    }

    /// <summary>Even-odd point in polygon over all contours of one face (holes are contours too).</summary>
    private static bool ContainsEvenOdd((double U, double V)[][] contours, double u, double v)
    {
        var inside = false;
        foreach (var contour in contours)
        {
            for (int index = 0, previous = contour.Length - 1; index < contour.Length; previous = index++)
            {
                var (ui, vi) = contour[index];
                var (uj, vj) = contour[previous];
                if ((vi > v) != (vj > v) && u < (uj - ui) * (v - vi) / (vj - vi) + ui)
                    inside = !inside;
            }
        }
        return inside;
    }

    private static double MeshVolume(RvmMesh mesh)
    {
        double volume = 0;
        for (var index = 0; index + 2 < mesh.Triangles.Length; index += 3)
        {
            var a = D3.From(mesh.Vertices[mesh.Triangles[index]]);
            var b = D3.From(mesh.Vertices[mesh.Triangles[index + 1]]);
            var c = D3.From(mesh.Vertices[mesh.Triangles[index + 2]]);
            volume += a.Dot(b.Cross(c)) / 6;
        }
        return volume;
    }

    /// <summary>
    /// Greedy maximal boxes: from each free inside cell, in the order's scan order, grow along the
    /// order's first axis, then the second, then the third, as far as every cell is free and inside.
    /// </summary>
    private static List<Cuboid> Merge(bool[] inside, int[] size, int[] order)
    {
        var used = new bool[inside.Length];
        var cuboids = new List<Cuboid>();
        var cell = new int[3];
        bool Free(int[] low, int[] high)
        {
            for (var i = low[0]; i < high[0]; i++)
            for (var j = low[1]; j < high[1]; j++)
            for (var k = low[2]; k < high[2]; k++)
            {
                var index = Index(size, i, j, k);
                if (!inside[index] || used[index])
                    return false;
            }
            return true;
        }

        for (var a = 0; a < size[order[0]]; a++)
        for (var b = 0; b < size[order[1]]; b++)
        for (var c = 0; c < size[order[2]]; c++)
        {
            cell[order[0]] = a;
            cell[order[1]] = b;
            cell[order[2]] = c;
            var start = Index(size, cell[0], cell[1], cell[2]);
            if (!inside[start] || used[start])
                continue;
            var low = (int[])cell.Clone();
            var high = new[] { cell[0] + 1, cell[1] + 1, cell[2] + 1 };
            foreach (var axis in order)
            {
                while (high[axis] < size[axis])
                {
                    var slabLow = (int[])low.Clone();
                    var slabHigh = (int[])high.Clone();
                    slabLow[axis] = high[axis];
                    slabHigh[axis] = high[axis] + 1;
                    if (!Free(slabLow, slabHigh))
                        break;
                    high[axis]++;
                }
            }
            for (var i = low[0]; i < high[0]; i++)
            for (var j = low[1]; j < high[1]; j++)
            for (var k = low[2]; k < high[2]; k++)
                used[Index(size, i, j, k)] = true;
            cuboids.Add(new Cuboid(low, high));
        }
        return cuboids;
    }
}
