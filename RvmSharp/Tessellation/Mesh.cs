namespace RvmSharp.Tessellation
{
    using System;
    using System.Linq;
    using System.Numerics;
    
    public class Mesh
    {
        public readonly float Error;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly int[] Triangles;

        public Mesh(float[] vertexData, float[] normalData, int[] triangleData, float error)
        {
            Error = error;
            if (vertexData.Length != normalData.Length)
                throw new ArgumentException("Vertex and normal arrays must have equal length");

            Vertices = new Vector3[vertexData.Length / 3];
            Normals = new Vector3[normalData.Length / 3];
            for (var i = 0; i < vertexData.Length / 3; i++)
            {
                Vertices[i] = new Vector3(vertexData[i * 3], vertexData[i * 3 + 1], vertexData[i * 3 + 2]);
                Normals[i] = new Vector3(normalData[i * 3], normalData[i * 3 + 1], normalData[i * 3 + 2]);
            }

            Triangles = new int[triangleData.Length];
            Array.Copy(triangleData, Triangles, triangleData.Length);
        }

        public Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles, float error)
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
            for (var i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = Vector3.Transform(Vertices[i], matrix);
                Normals[i] = Vector3.Normalize(Vector3.TransformNormal(Normals[i], matrix));
            }
        }

        public static Mesh Merge(Mesh mesh1, Mesh mesh2)
        {
            var mesh1VertexCount = mesh1.Vertices.Length;
            var vertices = mesh1.Vertices.Concat(mesh2.Vertices).ToArray();
            var normals = mesh1.Normals.Concat(mesh2.Normals).ToArray();
            var triangles = mesh1.Triangles.Concat(mesh2.Triangles.Select(t => t + mesh1VertexCount)).ToArray();
            var error = Math.Max(mesh1.Error, mesh2.Error);
            return new Mesh(vertices, normals, triangles, error);
        }
    }
}
