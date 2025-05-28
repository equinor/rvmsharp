namespace RvmSharp.Tessellation;

using System.Numerics;

public static class TessellationHelpers
{
    public static int QuadIndices(int[] indices, int l, int o, int v0, int v1, int v2, int v3)
    {
        indices[l] = o + v0;
        indices[l + 1] = o + v1;
        indices[l + 2] = o + v2;

        indices[l + 3] = o + v2;
        indices[l + 4] = o + v3;
        indices[l + 5] = o + v0;
        return l + 6;
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
        normals[l + 1] = normal.Y;
        normals[l + 2] = normal.Z;
        vertices[l] = point.X;
        vertices[l + 1] = point.Y;
        vertices[l + 2] = point.Z;
        return l + 3;
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
        normals[l + 1] = ny;
        normals[l + 2] = nz;
        vertices[l] = px;
        vertices[l + 1] = py;
        vertices[l + 2] = pz;
        return l + 3;
    }
}
