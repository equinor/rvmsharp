namespace CadRevealComposer.Tessellation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

public class Mesh : IEquatable<Mesh>
{
    public float Error { get; }

    /// <summary>
    /// Vertices are only positions Vector3 for now. We dont support normals or uvs.
    /// </summary>
    public Vector3[] Vertices => _vertices;

    public uint[] Indices => _indices;

    public int TriangleCount => _indices.Length / 3;

    private readonly Vector3[] _vertices;
    private readonly uint[] _indices;

    public Mesh(IReadOnlyList<float> vertexes, int[] indexes, float error)
    {
        Error = error;

        _vertices = new Vector3[vertexes.Count / 3];
        for (var i = 0; i < vertexes.Count / 3; i++)
        {
            _vertices[i] = new Vector3(vertexes[i * 3], vertexes[i * 3 + 1], vertexes[i * 3 + 2]);
        }

        _indices = new uint[indexes.Length];
        Array.Copy(indexes, _indices, indexes.Length);
    }

    /// <summary>
    /// Create a mesh using vertives, indices and error
    /// Note: Verticies and indices are referenced and not copied
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="indices"></param>
    /// <param name="error"></param>
    public Mesh(Vector3[] vertices, uint[] indices, float error)
    {
        Error = error;
        _vertices = vertices;
        _indices = indices;
    }

    public Mesh Clone()
    {
        return new Mesh(_vertices.ToArray(), _indices.ToArray(), Error);
    }

    /// <summary>
    /// Calculates a BoundingBox of the Mesh.
    /// Takes a transform as input and applies to the mesh data.
    /// </summary>
    /// <param name="transform">Optionally add a transform to the mesh while calculating the bounding box.</param>
    /// <returns>A Bounding Box</returns>
    /// <exception cref="Exception">Throws if the Mesh has 0 vertices.</exception>
    public BoundingBox CalculateAxisAlignedBoundingBox(Matrix4x4? transform)
    {
        var vertices = this._vertices;

        if (vertices.Length == 0)
            throw new Exception("Cannot find BoundingBox of a Mesh with 0 Vertices.");

        Vector3 min = Vector3.One * float.MaxValue;
        Vector3 max = Vector3.One * float.MinValue;
        if (transform is not null and { IsIdentity: false }) // Skip applying the transform if its an identity transform.
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

    /// <summary>
    /// Applies the Matrix4x4 to the current mesh instance
    /// </summary>
    /// <param name="matrix"></param>
    public void Apply(Matrix4x4 matrix)
    {
        for (var i = 0; i < _vertices.Length; i++)
        {
            var newVertex = Vector3.Transform(_vertices[i], matrix);

            Debug.Assert(float.IsFinite(newVertex.X));
            Debug.Assert(float.IsFinite(newVertex.Y));
            Debug.Assert(float.IsFinite(newVertex.Z));

            _vertices[i] = newVertex;
        }
    }

    #region Equality Comparers

    /// <summary>
    /// Compare this Mesh with another Mesh by values (Sequence equals for collections etc)
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
            && Vertices.SequenceEqual(other.Vertices)
            && Indices.SequenceEqual(other.Indices);
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
        var indicesHashCode = GetStructuralHashCode(_indices);

        return HashCode.Combine(errorHashCode, verticesHashCode, indicesHashCode);

        // Helper to get structural (ListContent-based) hash code
        static int GetStructuralHashCode<T>(IReadOnlyList<T> input)
            where T : IEquatable<T>
        {
            return ((IStructuralEquatable)input).GetHashCode(EqualityComparer<T>.Default);
        }
    }

    #endregion
}
