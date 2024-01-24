namespace RvmSharp.Tessellation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// this class is similar to CadRevealComposer.Tessellation.Mesh
// CadRevealComposer and RvmSharp packages are completely independent
// Conversion of RvmMesh to Mesh via CadRevealRvmProvider
public class RvmMesh : IEquatable<RvmMesh>
{
    public static RvmMesh Empty { get; } =
        new RvmMesh(Array.Empty<float>(), Array.Empty<float>(), Array.Empty<int>(), 0);

    public float Error { get; }

    public Vector3[] Vertices { get; }

    public Vector3[] Normals { get; }

    public uint[] Triangles { get; }

    public RvmMesh(IReadOnlyList<float> vertexData, IReadOnlyList<float> normalData, int[] triangleData, float error)
    {
        Error = error;
        if (vertexData.Count != normalData.Count)
            throw new ArgumentException("Vertex and normal arrays must have equal length");

        Vertices = new Vector3[vertexData.Count / 3];
        Normals = new Vector3[normalData.Count / 3];
        for (var i = 0; i < vertexData.Count / 3; i++)
        {
            Vertices[i] = new Vector3(vertexData[i * 3], vertexData[i * 3 + 1], vertexData[i * 3 + 2]);
            Normals[i] = new Vector3(normalData[i * 3], normalData[i * 3 + 1], normalData[i * 3 + 2]);
        }

        Triangles = new uint[triangleData.Length];
        Array.Copy(triangleData, Triangles, triangleData.Length);
    }

    public RvmMesh(Vector3[] vertices, Vector3[] normals, uint[] triangles, float error)
    {
        if (vertices.Length != normals.Length)
            throw new ArgumentException("Vertex and normal arrays must have equal length");

        Error = error;
        Vertices = vertices;
        Normals = normals;
        Triangles = triangles;
    }

    public void Apply(Matrix4x4 matrix)
    {
        // Transforming mesh normals requires some extra calculations.
        // https://web.archive.org/web/20210628111622/https://paroj.github.io/gltut/Illumination/Tut09%20Normal%20Transformation.html
        if (!Matrix4x4.Invert(matrix, out var matrixInverted))
            throw new ArgumentException($"Could not invert matrix {matrix}");
        var matrixInvertedTransposed = Matrix4x4.Transpose(matrixInverted);

        for (var i = 0; i < Vertices.Length; i++)
        {
            Vertices[i] = Vector3.Transform(Vertices[i], matrix);
            Normals[i] = Vector3.Normalize(Vector3.TransformNormal(Normals[i], matrixInvertedTransposed));
        }
    }

    public static RvmMesh Merge(RvmMesh mesh1, RvmMesh mesh2)
    {
        var mesh1VertexCount = (uint)mesh1.Vertices.Length;
        var vertices = mesh1.Vertices.Concat(mesh2.Vertices).ToArray();
        var normals = mesh1.Normals.Concat(mesh2.Normals).ToArray();
        var triangles = mesh1.Triangles.Concat(mesh2.Triangles.Select(t => t + mesh1VertexCount)).ToArray();
        var error = Math.Max(mesh1.Error, mesh2.Error);
        return new RvmMesh(vertices, normals, triangles, error);
    }

    #region Equality Comparers

    /// <summary>
    /// Compare this Mesh with another Mesh by values (Sequence equals for collections etc. Has a float tolerance, so there might be issues with  tiny meshes.
    /// </summary>
    public bool Equals(RvmMesh? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Error.Equals(other.Error)
            && Vertices.SequenceEqual(other.Vertices, new ToleranceVector3EqualityComparer(0.001f))
            && Normals.SequenceEqual(other.Normals, new ToleranceVector3EqualityComparer(tolerance: 0.001f))
            && Triangles.SequenceEqual(other.Triangles);
    }

    public override bool Equals(object? obj)
    {
        if (obj is RvmMesh other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var errorHashCode = Error.GetHashCode();
        var verticesHashCode = GetStructuralHashCode(Vertices);
        var normalsHashCode = GetStructuralHashCode(Normals);
        var trianglesHashCode = GetStructuralHashCode(Triangles);
        unchecked
        {
            // https://stackoverflow.com/a/1646913 (Replacement for HashCode.Combine since its not available on dotnet standard 2.0)
            int hash = 17;
            hash = hash * 31 + errorHashCode;
            hash = hash * 31 + verticesHashCode;
            hash = hash * 31 + normalsHashCode;
            hash = hash * 31 + trianglesHashCode;
            return hash;
        }

        // Helper to get structural (ListContent-based) hash code
        static int GetStructuralHashCode<T>(IReadOnlyList<T> input)
            where T : IEquatable<T>
        {
            return ((IStructuralEquatable)input).GetHashCode(EqualityComparer<T>.Default);
        }
    }

    private class ToleranceVector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private readonly float _tolerance;

        public ToleranceVector3EqualityComparer(float tolerance = 0.001f)
        {
            _tolerance = tolerance;
        }

        public bool Equals(Vector3 x, Vector3 y)
        {
            return Math.Abs(x.X - y.X) < _tolerance
                && Math.Abs(x.Y - y.Y) < _tolerance
                && Math.Abs(x.Z - y.Z) < _tolerance;
        }

        public int GetHashCode(Vector3 obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + obj.X.GetHashCode();
                hash = hash * 31 + obj.Y.GetHashCode();
                hash = hash * 31 + obj.Z.GetHashCode();
                return hash;
            }
        }
    }

    #endregion
}
