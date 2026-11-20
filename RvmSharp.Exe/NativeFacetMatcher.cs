namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Primitives;

/// <summary>Which transforms may relate two facet groups that share a template.</summary>
internal enum FacetMatchMode
{
    /// <summary>Rotation and translation.</summary>
    Rigid,

    /// <summary>Rotation and translation, optionally with one reflection.</summary>
    RigidOrMirrored,

    /// <summary>As <see cref="RigidOrMirrored"/> with one uniform scale factor.</summary>
    Similar,

    /// <summary>Any affine map. Only an upper bound on per-axis stretch reuse; used for measurement.</summary>
    Affine,
}

/// <summary>Column-vector 3x3 matrix: <c>result = M * vector</c>.</summary>
internal readonly record struct Linear3(
    double M11,
    double M12,
    double M13,
    double M21,
    double M22,
    double M23,
    double M31,
    double M32,
    double M33
)
{
    internal static Linear3 Identity { get; } = new(1, 0, 0, 0, 1, 0, 0, 0, 1);

    internal double Determinant =>
        M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31);

    public static Linear3 operator *(Linear3 a, Linear3 b) =>
        new(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
        );

    internal Linear3 Scaled(double factor) =>
        new(
            M11 * factor,
            M12 * factor,
            M13 * factor,
            M21 * factor,
            M22 * factor,
            M23 * factor,
            M31 * factor,
            M32 * factor,
            M33 * factor
        );

    /// <summary>This matrix applied after negating the input's X coordinate.</summary>
    internal Linear3 WithMirroredInput() => new(-M11, M12, M13, -M21, M22, M23, -M31, M32, M33);
}

/// <summary>Maps a representative's centred vertices onto a member's centred vertices.</summary>
internal readonly record struct FacetFit(Linear3 Linear, double MaxResidual)
{
    internal static FacetFit Identity { get; } = new(Linear3.Identity, 0);

    internal bool Mirrored => Linear.Determinant < 0;
}

/// <summary>
/// The ordered contour vertices of one facet group in world metres, relative to their centroid.
/// Vertex order is the source order, so two shapes are compared index by index.
/// </summary>
internal sealed class FacetShape
{
    private float[]? _radial;
    private float[]? _mahalanobis;
    private double[]? _inverseScatter;

    private FacetShape(long structureKey, float[] points, Vector3d center, float radius, float rmsRadius)
    {
        StructureKey = structureKey;
        Points = points;
        Center = center;
        Radius = radius;
        RmsRadius = rmsRadius;
    }

    /// <summary>Hash of the polygon, contour and vertex-count layout.</summary>
    internal long StructureKey { get; }

    /// <summary>Interleaved XYZ offsets from <see cref="Center"/>.</summary>
    internal float[] Points { get; }

    internal Vector3d Center { get; }

    internal float Radius { get; }

    internal float RmsRadius { get; }

    internal int VertexCount => Points.Length / 3;

    /// <summary>Index of the source this shape came from; first part of the deterministic order.</summary>
    internal int Source { get; init; }

    /// <summary>Position within its source; second part of the deterministic order.</summary>
    internal int Ordinal { get; init; }

    /// <summary>How many facet groups this shape stands for.</summary>
    internal int Weight { get; init; } = 1;

    /// <summary>Distance of every vertex from the centroid, in vertex order.</summary>
    internal float[] Radial
    {
        get
        {
            if (_radial != null)
                return _radial;
            var radial = new float[VertexCount];
            for (var index = 0; index < radial.Length; index++)
            {
                double x = Points[index * 3],
                    y = Points[index * 3 + 1],
                    z = Points[index * 3 + 2];
                radial[index] = (float)Math.Sqrt(x * x + y * y + z * z);
            }
            return _radial = radial;
        }
    }

    /// <summary>Smallest standard deviation along a principal axis; zero for planar shapes.</summary>
    internal double MinimumSpread { get; private set; } = double.NaN;

    /// <summary>Inverse of the vertex scatter matrix, or null when the shape is too flat to invert.</summary>
    internal double[]? InverseScatter(double flatness)
    {
        if (double.IsNaN(MinimumSpread))
            PrepareScatter();
        return MinimumSpread > flatness ? _inverseScatter : null;
    }

    /// <summary>Affine-invariant distance of every vertex from the centroid, in vertex order.</summary>
    internal float[] Mahalanobis
    {
        get
        {
            if (_mahalanobis != null)
                return _mahalanobis;
            var inverse = _inverseScatter ?? throw new InvalidOperationException("Shape is not full rank.");
            var count = VertexCount;
            var result = new float[count];
            for (var index = 0; index < count; index++)
            {
                double x = Points[index * 3],
                    y = Points[index * 3 + 1],
                    z = Points[index * 3 + 2];
                var value =
                    x * (inverse[0] * x + inverse[1] * y + inverse[2] * z)
                    + y * (inverse[3] * x + inverse[4] * y + inverse[5] * z)
                    + z * (inverse[6] * x + inverse[7] * y + inverse[8] * z);
                result[index] = (float)Math.Sqrt(Math.Max(value, 0) * count);
            }
            return _mahalanobis = result;
        }
    }

    internal static FacetShape? Create(RvmFacetGroup facetGroup, int source = 0, int ordinal = 0)
    {
        var count = 0;
        var key = 0x9E3779B97F4A7C15UL;
        key = Mix(key + (ulong)facetGroup.Polygons.Length);
        foreach (var polygon in facetGroup.Polygons)
        {
            key = Mix(key + (ulong)polygon.Contours.Length);
            foreach (var contour in polygon.Contours)
            {
                key = Mix(key + (ulong)contour.Vertices.Length);
                count += contour.Vertices.Length;
            }
        }
        if (count == 0)
            return null;

        var matrix = facetGroup.Matrix;
        var world = new double[count * 3];
        double sumX = 0,
            sumY = 0,
            sumZ = 0;
        var offset = 0;
        foreach (var polygon in facetGroup.Polygons)
        foreach (var contour in polygon.Contours)
        foreach (var (vertex, _) in contour.Vertices)
        {
            double x = vertex.X,
                y = vertex.Y,
                z = vertex.Z;
            var worldX = x * matrix.M11 + y * matrix.M21 + z * matrix.M31 + matrix.M41;
            var worldY = x * matrix.M12 + y * matrix.M22 + z * matrix.M32 + matrix.M42;
            var worldZ = x * matrix.M13 + y * matrix.M23 + z * matrix.M33 + matrix.M43;
            world[offset++] = worldX;
            world[offset++] = worldY;
            world[offset++] = worldZ;
            sumX += worldX;
            sumY += worldY;
            sumZ += worldZ;
        }

        var center = new Vector3d(sumX / count, sumY / count, sumZ / count);
        return FromWorld(unchecked((long)key), world, center, source, ordinal, 1);
    }

    /// <summary>Builds a shape from points that are already relative to their centroid.</summary>
    internal static FacetShape FromCentered(
        long structureKey,
        float[] points,
        Vector3d center,
        int source,
        int ordinal,
        int weight
    )
    {
        double squared = 0,
            maximum = 0;
        for (var index = 0; index < points.Length; index += 3)
        {
            double x = points[index],
                y = points[index + 1],
                z = points[index + 2];
            var lengthSquared = x * x + y * y + z * z;
            squared += lengthSquared;
            maximum = Math.Max(maximum, lengthSquared);
        }
        return new FacetShape(
            structureKey,
            points,
            center,
            (float)Math.Sqrt(maximum),
            (float)Math.Sqrt(squared / (points.Length / 3))
        )
        {
            Source = source,
            Ordinal = ordinal,
            Weight = weight,
        };
    }

    private static FacetShape FromWorld(
        long structureKey,
        double[] world,
        Vector3d center,
        int source,
        int ordinal,
        int weight
    )
    {
        var points = new float[world.Length];
        for (var index = 0; index < world.Length; index += 3)
        {
            points[index] = (float)(world[index] - center.X);
            points[index + 1] = (float)(world[index + 1] - center.Y);
            points[index + 2] = (float)(world[index + 2] - center.Z);
        }
        return FromCentered(structureKey, points, center, source, ordinal, weight);
    }

    private void PrepareScatter()
    {
        Span<double> scatter = stackalloc double[9];
        Span<double> vectors = stackalloc double[9];
        for (var index = 0; index < Points.Length; index += 3)
        {
            double x = Points[index],
                y = Points[index + 1],
                z = Points[index + 2];
            scatter[0] += x * x;
            scatter[1] += x * y;
            scatter[2] += x * z;
            scatter[4] += y * y;
            scatter[5] += y * z;
            scatter[8] += z * z;
        }
        scatter[3] = scatter[1];
        scatter[6] = scatter[2];
        scatter[7] = scatter[5];
        Span<double> values = stackalloc double[9];
        scatter.CopyTo(values);
        FacetAlignment.SymmetricEigen(values, vectors, 3);
        var smallest = Math.Min(values[0], Math.Min(values[4], values[8]));
        MinimumSpread = Math.Sqrt(Math.Max(smallest, 0) / VertexCount);
        if (smallest <= 0)
            return;

        var inverse = new double[9];
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 3; column++)
        {
            double sum = 0;
            for (var axis = 0; axis < 3; axis++)
                sum += vectors[row * 3 + axis] * vectors[column * 3 + axis] / values[axis * 4];
            inverse[row * 3 + column] = sum;
        }
        _inverseScatter = inverse;
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}

internal readonly record struct Vector3d(double X, double Y, double Z);

internal static class FacetAlignment
{
    /// <summary>
    /// Fits <paramref name="reference"/> onto <paramref name="candidate"/> about their centroids and
    /// accepts when every vertex lands within <paramref name="tolerance"/> metres.
    /// </summary>
    internal static bool TryFit(
        FacetShape reference,
        FacetShape candidate,
        FacetMatchMode mode,
        double tolerance,
        out FacetFit fit
    )
    {
        fit = default;
        var a = reference.Points;
        var b = candidate.Points;
        if (a.Length != b.Length)
            return false;

        Span<double> cross = stackalloc double[9];
        for (var index = 0; index < a.Length; index += 3)
        {
            double ax = a[index],
                ay = a[index + 1],
                az = a[index + 2];
            double bx = b[index],
                by = b[index + 1],
                bz = b[index + 2];
            cross[0] += ax * bx;
            cross[1] += ax * by;
            cross[2] += ax * bz;
            cross[3] += ay * bx;
            cross[4] += ay * by;
            cross[5] += ay * bz;
            cross[6] += az * bx;
            cross[7] += az * by;
            cross[8] += az * bz;
        }

        if (
            mode == FacetMatchMode.Affine
            && reference.InverseScatter(8 * tolerance) is { } inverse
            && candidate.InverseScatter(8 * tolerance) != null
        )
        {
            // Least squares: (sum b a^T) (sum a a^T)^-1, and sum b a^T is the transpose of cross.
            var linear = new Linear3(
                cross[0] * inverse[0] + cross[3] * inverse[3] + cross[6] * inverse[6],
                cross[0] * inverse[1] + cross[3] * inverse[4] + cross[6] * inverse[7],
                cross[0] * inverse[2] + cross[3] * inverse[5] + cross[6] * inverse[8],
                cross[1] * inverse[0] + cross[4] * inverse[3] + cross[7] * inverse[6],
                cross[1] * inverse[1] + cross[4] * inverse[4] + cross[7] * inverse[7],
                cross[1] * inverse[2] + cross[4] * inverse[5] + cross[7] * inverse[8],
                cross[2] * inverse[0] + cross[5] * inverse[3] + cross[8] * inverse[6],
                cross[2] * inverse[1] + cross[5] * inverse[4] + cross[8] * inverse[7],
                cross[2] * inverse[2] + cross[5] * inverse[5] + cross[8] * inverse[8]
            );
            return Verify(linear, a, b, tolerance, out fit);
        }

        var scale =
            mode is FacetMatchMode.Similar or FacetMatchMode.Affine && reference.RmsRadius > 0
                ? (double)candidate.RmsRadius / reference.RmsRadius
                : 1.0;
        if (Verify(Rotation(cross).Scaled(scale), a, b, tolerance, out fit))
            return true;
        if (mode == FacetMatchMode.Rigid)
            return false;

        cross[0] = -cross[0];
        cross[1] = -cross[1];
        cross[2] = -cross[2];
        return Verify(Rotation(cross).Scaled(scale).WithMirroredInput(), a, b, tolerance, out fit);
    }

    /// <summary>Largest distance between <c>linear * a</c> and <c>b</c>, or failure once it exceeds the tolerance.</summary>
    internal static bool Verify(Linear3 linear, float[] a, float[] b, double tolerance, out FacetFit fit)
    {
        var limit = tolerance * tolerance;
        double worst = 0;
        for (var index = 0; index < a.Length; index += 3)
        {
            double ax = a[index],
                ay = a[index + 1],
                az = a[index + 2];
            var dx = linear.M11 * ax + linear.M12 * ay + linear.M13 * az - b[index];
            var dy = linear.M21 * ax + linear.M22 * ay + linear.M23 * az - b[index + 1];
            var dz = linear.M31 * ax + linear.M32 * ay + linear.M33 * az - b[index + 2];
            var squared = dx * dx + dy * dy + dz * dz;
            if (squared > limit)
            {
                fit = default;
                return false;
            }
            worst = Math.Max(worst, squared);
        }
        fit = new FacetFit(linear, Math.Sqrt(worst));
        return true;
    }

    /// <summary>
    /// Horn's closed-form absolute orientation: the proper rotation R maximising sum (R a) . b,
    /// given <c>cross[r, c] = sum a_r b_c</c>.
    /// </summary>
    internal static Linear3 Rotation(ReadOnlySpan<double> cross)
    {
        double sxx = cross[0],
            sxy = cross[1],
            sxz = cross[2];
        double syx = cross[3],
            syy = cross[4],
            syz = cross[5];
        double szx = cross[6],
            szy = cross[7],
            szz = cross[8];
        Span<double> matrix =
        [
            sxx + syy + szz,
            syz - szy,
            szx - sxz,
            sxy - syx,
            syz - szy,
            sxx - syy - szz,
            sxy + syx,
            szx + sxz,
            szx - sxz,
            sxy + syx,
            -sxx + syy - szz,
            syz + szy,
            sxy - syx,
            szx + sxz,
            syz + szy,
            -sxx - syy + szz,
        ];
        Span<double> vectors = stackalloc double[16];
        SymmetricEigen(matrix, vectors, 4);
        var best = 0;
        for (var index = 1; index < 4; index++)
        {
            if (matrix[index * 5] > matrix[best * 5])
                best = index;
        }

        double w = vectors[best],
            x = vectors[4 + best],
            y = vectors[8 + best],
            z = vectors[12 + best];
        var norm = Math.Sqrt(w * w + x * x + y * y + z * z);
        if (norm <= 0)
            return Linear3.Identity;
        w /= norm;
        x /= norm;
        y /= norm;
        z /= norm;
        return new Linear3(
            w * w + x * x - y * y - z * z,
            2 * (x * y - w * z),
            2 * (x * z + w * y),
            2 * (y * x + w * z),
            w * w - x * x + y * y - z * z,
            2 * (y * z - w * x),
            2 * (z * x - w * y),
            2 * (z * y + w * x),
            w * w - x * x - y * y + z * z
        );
    }

    /// <summary>
    /// Cyclic Jacobi eigendecomposition of a symmetric matrix. On return the diagonal of
    /// <paramref name="matrix"/> holds the eigenvalues and the columns of
    /// <paramref name="vectors"/> the matching eigenvectors.
    /// </summary>
    internal static void SymmetricEigen(Span<double> matrix, Span<double> vectors, int size)
    {
        vectors.Clear();
        for (var index = 0; index < size; index++)
            vectors[index * size + index] = 1;

        for (var sweep = 0; sweep < 64; sweep++)
        {
            double offDiagonal = 0,
                diagonal = 0;
            for (var row = 0; row < size; row++)
            {
                diagonal += matrix[row * size + row] * matrix[row * size + row];
                for (var column = row + 1; column < size; column++)
                    offDiagonal += matrix[row * size + column] * matrix[row * size + column];
            }
            if (offDiagonal <= 1e-30 * (diagonal + offDiagonal))
                return;

            for (var p = 0; p < size - 1; p++)
            for (var q = p + 1; q < size; q++)
            {
                var pivot = matrix[p * size + q];
                if (pivot == 0)
                    continue;
                var theta = (matrix[q * size + q] - matrix[p * size + p]) / (2 * pivot);
                var tangent = Math.Sign(theta) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1));
                if (theta == 0)
                    tangent = 1;
                var cosine = 1 / Math.Sqrt(tangent * tangent + 1);
                var sine = tangent * cosine;
                for (var k = 0; k < size; k++)
                {
                    var kp = matrix[k * size + p];
                    var kq = matrix[k * size + q];
                    matrix[k * size + p] = cosine * kp - sine * kq;
                    matrix[k * size + q] = sine * kp + cosine * kq;
                }
                for (var k = 0; k < size; k++)
                {
                    var pk = matrix[p * size + k];
                    var qk = matrix[q * size + k];
                    matrix[p * size + k] = cosine * pk - sine * qk;
                    matrix[q * size + k] = sine * pk + cosine * qk;
                }
                for (var k = 0; k < size; k++)
                {
                    var kp = vectors[k * size + p];
                    var kq = vectors[k * size + q];
                    vectors[k * size + p] = cosine * kp - sine * kq;
                    vectors[k * size + q] = sine * kp + cosine * kq;
                }
            }
        }
    }
}

/// <summary>One template: a representative shape and every shape that fits it.</summary>
internal sealed class FacetClass(FacetShape representative)
{
    internal FacetShape Representative { get; } = representative;

    internal List<(FacetShape Shape, FacetFit Fit)> Members { get; } = [(representative, FacetFit.Identity)];
}

/// <summary>
/// Groups facet groups that are the same part placed differently. Shapes are compared vertex by
/// vertex in source order, so matching needs no canonical pose and works for symmetric, planar
/// and very small parts alike.
/// </summary>
internal static class NativeFacetMatcher
{
    /// <summary>Largest number of earlier templates one shape is compared against in the scale-free modes.</summary>
    internal const int CandidateLimit = 4096;

    /// <summary>
    /// Clusters <paramref name="shapes"/> deterministically: within one vertex layout, shapes are
    /// visited by ascending size and each joins the nearest earlier template it fits.
    /// </summary>
    internal static List<FacetClass> Cluster(
        IReadOnlyList<FacetShape> shapes,
        FacetMatchMode mode,
        double tolerance,
        int maxDegreeOfParallelism = -1
    )
    {
        var buckets = shapes
            .GroupBy(shape => (shape.StructureKey, shape.VertexCount))
            .OrderBy(group => group.Key.VertexCount)
            .ThenBy(group => group.Key.StructureKey)
            .Select(group => group.ToArray())
            .ToArray();
        var results = new List<FacetClass>[buckets.Length];
        Parallel.For(
            0,
            buckets.Length,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            index => results[index] = ClusterBucket(buckets[index], mode, tolerance)
        );
        return results.SelectMany(classes => classes).ToList();
    }

    private static List<FacetClass> ClusterBucket(FacetShape[] bucket, FacetMatchMode mode, double tolerance)
    {
        // Affine comparison needs an invertible scatter matrix; flatter shapes fall back to similarity.
        var flat =
            mode == FacetMatchMode.Affine
                ? bucket.Where(shape => shape.InverseScatter(8 * tolerance) == null).ToArray()
                : [];
        if (flat.Length == 0)
            return Sweep(bucket, mode, tolerance);
        var classes = Sweep(bucket.Except(flat).ToArray(), FacetMatchMode.Affine, tolerance);
        classes.AddRange(Sweep(flat, FacetMatchMode.Similar, tolerance));
        return classes;
    }

    private static List<FacetClass> Sweep(FacetShape[] bucket, FacetMatchMode mode, double tolerance)
    {
        var keys = new double[bucket.Length];
        var windows = new double[bucket.Length];
        for (var index = 0; index < bucket.Length; index++)
            (keys[index], windows[index]) = SortKey(bucket[index], mode, tolerance);
        var order = Enumerable.Range(0, bucket.Length).ToArray();
        Array.Sort(
            order,
            (left, right) =>
            {
                var comparison = keys[left].CompareTo(keys[right]);
                if (comparison == 0)
                    comparison = bucket[left].Source.CompareTo(bucket[right].Source);
                return comparison != 0 ? comparison : bucket[left].Ordinal.CompareTo(bucket[right].Ordinal);
            }
        );

        var classes = new List<FacetClass>();
        var classKeys = new List<double>();
        foreach (var index in order)
        {
            var shape = bucket[index];
            var matched = false;
            var oldest = mode is FacetMatchMode.Rigid or FacetMatchMode.RigidOrMirrored
                ? 0
                : Math.Max(0, classes.Count - CandidateLimit);
            for (var candidate = classes.Count - 1; candidate >= oldest; candidate--)
            {
                if (keys[index] - classKeys[candidate] > windows[index])
                    break;
                var representative = classes[candidate].Representative;
                if (
                    !ProfilesAgree(representative, shape, mode, tolerance, windows[index])
                    || !FacetAlignment.TryFit(representative, shape, mode, tolerance, out var fit)
                )
                    continue;
                classes[candidate].Members.Add((shape, fit));
                matched = true;
                break;
            }
            if (matched)
                continue;
            classes.Add(new FacetClass(shape));
            classKeys.Add(keys[index]);
        }
        return classes;
    }

    /// <summary>
    /// A scalar that two matching shapes share to within the returned window. For the rigid modes
    /// the window is exact: a fit within the tolerance cannot change any centroid distance by more.
    /// </summary>
    private static (double Key, double Window) SortKey(FacetShape shape, FacetMatchMode mode, double tolerance)
    {
        const double slack = 1e-9;
        switch (mode)
        {
            case FacetMatchMode.Rigid or FacetMatchMode.RigidOrMirrored:
                return (shape.Radius, tolerance + slack);
            case FacetMatchMode.Similar:
                return shape.RmsRadius > 0
                    ? (shape.Radius / shape.RmsRadius, tolerance / shape.RmsRadius + slack)
                    : (0, slack);
            default:
                var key = shape.Mahalanobis.Max();
                return (key, 3 * tolerance * (1 + key) / shape.MinimumSpread + slack);
        }
    }

    private static bool ProfilesAgree(
        FacetShape reference,
        FacetShape candidate,
        FacetMatchMode mode,
        double tolerance,
        double window
    )
    {
        if (mode == FacetMatchMode.Affine)
            return Agree(reference.Mahalanobis, 1, candidate.Mahalanobis, 1, window);
        if (mode == FacetMatchMode.Similar)
        {
            if (reference.RmsRadius <= 0 || candidate.RmsRadius <= 0)
                return reference.RmsRadius <= 0 && candidate.RmsRadius <= 0;
            return Agree(
                reference.Radial,
                1.0 / reference.RmsRadius,
                candidate.Radial,
                1.0 / candidate.RmsRadius,
                window
            );
        }
        return Agree(reference.Radial, 1, candidate.Radial, 1, window);
    }

    private static bool Agree(float[] first, double firstScale, float[] second, double secondScale, double window)
    {
        for (var index = 0; index < first.Length; index++)
        {
            if (Math.Abs(first[index] * firstScale - second[index] * secondScale) > window)
                return false;
        }
        return true;
    }
}
