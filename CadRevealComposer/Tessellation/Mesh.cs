namespace CadRevealComposer.Tessellation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Mesh : IEquatable<Mesh>
{
    public static Mesh Empty { get; } = new Mesh(Array.Empty<float>(), Array.Empty<float>(), Array.Empty<int>(), 0);

    public float Error { get; }

    public Vector3[] Vertices => _vertices;

    public Vector3[] Normals => _normals;

    public uint[] Triangles => _triangles;

    public int TriangleCount => _triangles.Length / 3;

    private readonly Vector3[] _vertices;
    private readonly Vector3[] _normals;
    private readonly uint[] _triangles;

    public Mesh(IReadOnlyList<float> vertexData, IReadOnlyList<float> normalData, int[] triangleData, float error)
    {
        Error = error;
        if (vertexData.Count != normalData.Count)
            throw new ArgumentException("Vertex and normal arrays must have equal length");

        _vertices = new Vector3[vertexData.Count / 3];
        _normals = new Vector3[normalData.Count / 3];
        for (var i = 0; i < vertexData.Count / 3; i++)
        {
            _vertices[i] = new Vector3(vertexData[i * 3], vertexData[i * 3 + 1], vertexData[i * 3 + 2]);
            _normals[i] = new Vector3(normalData[i * 3], normalData[i * 3 + 1], normalData[i * 3 + 2]);
        }

        _triangles = new uint[triangleData.Length];
        Array.Copy(triangleData, _triangles, triangleData.Length);
    }

    public Mesh(Vector3[] vertices, Vector3[] normals, uint[] triangles, float error)
    {
        if (vertices.Length != normals.Length)
            throw new ArgumentException("Vertex and normal arrays must have equal length");

        Error = error;
        _vertices = vertices;
        _normals = normals;
        _triangles = triangles;
    }

    /// <summary>
    /// Calculates a BoundingBox of the Mesh.
    /// Takes a transform as input if needed.
    /// </summary>
    /// <param name="transform">Optionally add a transform to the bounding box.</param>
    /// <returns>A Bounding Box</returns>
    /// <exception cref="Exception">Throws if the Mesh has 0 vertices.</exception>
    public BoundingBox CalculateBoundingBox(Matrix4x4? transform)
    {
        var vertices = this._vertices;

        if (vertices.Length == 0)
            throw new Exception("Cannot find BoundingBox of a Mesh with 0 Vertices.");

        Vector3 min = Vector3.One * float.MaxValue;
        Vector3 max = Vector3.One * float.MinValue;
        if (transform is not null and { IsIdentity: false }) // Do not apply the transform if its an identity transform.
        {
            for (int i = 1; i < vertices.Length; i++)
            {
                var transformedVertice = Vector3.Transform(vertices[i], transform.Value);
                min = Vector3.Min(min, transformedVertice);
                max = Vector3.Max(max, transformedVertice);
            }
        }
        else
        {
            for (int i = 1; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
        }

        return new BoundingBox(min, max);
    }

    public void Apply(Matrix4x4 matrix)
    {
        // Transforming mesh normals requires some extra calculations.
        // https://web.archive.org/web/20210628111622/https://paroj.github.io/gltut/Illumination/Tut09%20Normal%20Transformation.html
        if (!Matrix4x4.Invert(matrix, out var matrixInverted))
            throw new ArgumentException($"Could not invert matrix {matrix}");
        var matrixInvertedTransposed = Matrix4x4.Transpose(matrixInverted);

        for (var i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = Vector3.Transform(_vertices[i], matrix);
            _normals[i] = Vector3.Normalize(Vector3.TransformNormal(_normals[i], matrixInvertedTransposed));
        }
    }

    public static Mesh Merge(Mesh mesh1, Mesh mesh2)
    {
        var mesh1VertexCount = (uint)mesh1.Vertices.Length;
        var vertices = mesh1.Vertices.Concat(mesh2.Vertices).ToArray();
        var normals = mesh1.Normals.Concat(mesh2.Normals).ToArray();
        var triangles = mesh1.Triangles.Concat(mesh2.Triangles.Select(t => t + mesh1VertexCount)).ToArray();
        var error = Math.Max(mesh1.Error, mesh2.Error);
        return new Mesh(vertices, normals, triangles, error);
    }


    #region Equality Comparers

    /// <summary>
    /// Compare this Mesh with another Mesh by values (Sequence equals for collections etc. Has a float tolerance, so there might be issues with  tiny meshes.
    /// </summary>
    public bool Equals(Mesh? other)
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
               && Vertices.SequenceEqual(other.Vertices, new ToleranceVector3EqualityComparer(tolerance: 0.001f))
               && Normals.SequenceEqual(other.Normals, new ToleranceVector3EqualityComparer(tolerance: 0.001f))
               && Triangles.SequenceEqual(other.Triangles);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Mesh other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var errorHashCode = Error.GetHashCode();
        var verticesHashCode = GetStructuralHashCode(_vertices);
        var normalsHashCode = GetStructuralHashCode(_normals);
        var trianglesHashCode = GetStructuralHashCode(_triangles);
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
        static int GetStructuralHashCode<T>(IReadOnlyList<T> input) where T : IEquatable<T>
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