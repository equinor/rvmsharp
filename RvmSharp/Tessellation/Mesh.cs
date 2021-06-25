namespace RvmSharp.Tessellation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    
    public class Mesh
    {
        public float Error { get; }

        public IReadOnlyList<Vector3> Vertices => _vertices;

        public IReadOnlyList<Vector3> Normals => _normals;

        public IReadOnlyList<int> Triangles => _triangles;

        private readonly Vector3[] _vertices;
        private readonly Vector3[] _normals;
        private readonly int[] _triangles;

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

            _triangles = new int[triangleData.Length];
            Array.Copy(triangleData, _triangles, triangleData.Length);
        }

        public Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles, float error)
        {
            if (vertices.Length != normals.Length)
                throw new ArgumentException("Vertex and normal arrays must have equal length");

            Error = error;
            _vertices = vertices;
            _normals = normals;
            _triangles = triangles;
        }

        public void Apply(Matrix4x4 matrix)
        {
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i] = Vector3.Transform(_vertices[i], matrix);
                _normals[i] = Vector3.Normalize(Vector3.TransformNormal(_normals[i], matrix));
            }
        }

        public static Mesh Merge(Mesh mesh1, Mesh mesh2)
        {
            var mesh1VertexCount = mesh1.Vertices.Count;
            var vertices = mesh1.Vertices.Concat(mesh2.Vertices).ToArray();
            var normals = mesh1.Normals.Concat(mesh2.Normals).ToArray();
            var triangles = mesh1.Triangles.Concat(mesh2.Triangles.Select(t => t + mesh1VertexCount)).ToArray();
            var error = Math.Max(mesh1.Error, mesh2.Error);
            return new Mesh(vertices, normals, triangles, error);
        }
    }
}

        public override int GetHashCode()
        {
            var errorHashCode = Error.GetHashCode();
            var verticesHashCode = ((IStructuralEquatable)Vertices).GetHashCode(EqualityComparer<float>.Default);
            var normalsHashCode = ((IStructuralEquatable)Normals).GetHashCode(EqualityComparer<float>.Default);
            var trianglesHashCode = ((IStructuralEquatable)Triangles).GetHashCode(EqualityComparer<float>.Default);
            unchecked
            {
                // https://stackoverflow.com/a/1646913 (Replacement for HashCode.Combine since its not available on dotnet standard 2.0)
                int hash = 17;
                hash = hash * 31 + errorHashCode.GetHashCode();
                hash = hash * 31 + verticesHashCode.GetHashCode();
                hash = hash * 31 + normalsHashCode.GetHashCode();
                hash = hash * 31 + trianglesHashCode.GetHashCode();
                return hash;
            }
        }
    }
}