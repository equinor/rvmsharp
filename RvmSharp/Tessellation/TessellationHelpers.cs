namespace RvmSharp.Tessellation;

using System;
using System.Numerics;

public static class TessellationHelpers
{
    private const int MinSamples = 3;
    private const int MaxSamples = 100;

    public static int QuadIndices(int[] indices, int l, int o, int v0, int v1, int v2, int v3)
    {
        indices[l++] = o + v0;
        indices[l++] = o + v1;
        indices[l++] = o + v2;

        indices[l++] = o + v2;
        indices[l++] = o + v3;
        indices[l++] = o + v0;
        return l;
    }

    public static int Vertex(Vector3[] normals, Vector3[] vertices, int l, Vector3 normal, Vector3 point)
    {
        normals[l] = normal;
        vertices[l] = point;
        return ++l;
    }

    public static int Vertex(
        Vector3[] normals,
        Vector3[] vertices,
        int l,
        float nx,
        float ny,
        float nz,
        float px,
        float py,
        float pz
    )
    {
        normals[l] = new Vector3(nx, ny, nz);
        vertices[l] = new Vector3(px, py, pz);
        return ++l;
    }

    public static int Vertex(float[] normals, float[] vertices, int l, Vector3 normal, Vector3 point)
    {
        normals[l] = normal.X;
        vertices[l++] = point.X;
        normals[l] = normal.Y;
        vertices[l++] = point.Y;
        normals[l] = normal.Z;
        vertices[l++] = point.Z;
        return l;
    }

    public static int Vertex(
        float[] normals,
        float[] vertices,
        int l,
        float nx,
        float ny,
        float nz,
        float px,
        float py,
        float pz
    )
    {
        normals[l] = nx;
        vertices[l++] = px;
        normals[l] = ny;
        vertices[l++] = py;
        normals[l] = nz;
        vertices[l++] = pz;
        return l;
    }

    /// <summary>
    /// Calculates the "maximum deviation" in the mesh from the "ideal" primitive.
    /// If we round a cylinder to N segment faces, this method gives us the distance from the extents of a the center
    /// of a flat face to the extents of a perfect cylinder.
    /// See: https://en.wikipedia.org/wiki/Sagitta_(geometry)
    /// </summary>
    public static float SagittaBasedError(double arc, float radius, float scale, int segments)
    {
        var lengthOfSagitta = scale * radius * (1.0f - Math.Cos(arc / segments)); // Length of sagitta
        //assert(s <= tolerance);
        return (float)lengthOfSagitta;
    }

    /// <summary>
    /// Calculates the amount of segments we need to represent this primitive within a given tolerance.
    /// </summary>
    /// <example>
    /// Example: A small cylinder with a tolerance of 0.1 might be represented with 8 sides, but a large cylinder might need 32
    /// </example>
    public static int SagittaBasedSegmentCount(double arc, float radius, float scale, float tolerance)
    {
        var maximumSagitta = tolerance;
        var samples = arc / Math.Acos(Math.Max(-1.0f, 1.0f - maximumSagitta / (scale * radius)));
        if (double.IsNaN(samples))
        {
            throw new Exception(
                $"Number of samples is calculated as NaN. Diagnostics: ({nameof(scale)}: {scale}, {nameof(arc)}: {arc}, {nameof(radius)}: {radius}, {nameof(tolerance)}: {tolerance} )"
            );
        }

        return Math.Min(MaxSamples, (int)(Math.Max(MinSamples, Math.Ceiling(samples))));
    }
}
