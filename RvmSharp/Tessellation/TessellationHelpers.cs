namespace RvmSharp.Tessellation;

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
}
