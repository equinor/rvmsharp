using System;
using System.Numerics;

namespace rvmsharp.Tessellator
{
    public class Mesh
    {
        public readonly float Error;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly int[] Triangles;

        public Mesh(float[] verexData, float[] normalData, int[] triangleData, float error)
        {
            Error = error;
            if (verexData.Length != normalData.Length)
                throw new ArgumentException("Vertex and normal arrays must have equial length");

            Vertices = new Vector3[verexData.Length / 3];
            Normals = new Vector3[normalData.Length / 3];
            for (var i = 0; i < verexData.Length / 3; i++)
            {
                Vertices[i] = new Vector3(verexData[i * 3], verexData[i * 3 + 1], verexData[i * 3 + 2]);
                Normals[i] = new Vector3(normalData[i * 3], normalData[i * 3 + 1], normalData[i * 3 + 2]);
            }

            Triangles = new int[triangleData.Length];
            Array.Copy(triangleData, Triangles, triangleData.Length);
        }

        public Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles, float error)
        {
            if (vertices.Length != normals.Length)
                throw new ArgumentException("Vertex and normal arrays must have equial length");

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
    }
}
