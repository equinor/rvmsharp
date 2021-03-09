using System;
using System.Numerics;

namespace rvmsharp.Tessellator
{
    public class Mesh
    {
        public float error;
        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] indices;

        public Mesh(float[] verexData, float[] normalData, int[] indexData, float error)
        {
            this.error = error;
            if (verexData.Length != normalData.Length)
                throw new ArgumentNullException("Normal buffer is not equials to vertex buffer");

            vertices = new Vector3[verexData.Length / 3];
            normals = new Vector3[normalData.Length / 3];
            for (var i = 0; i < verexData.Length / 3; i++)
            {
                vertices[i] = new Vector3(verexData[i * 3], verexData[i * 3 + 1], verexData[i * 3 + 2]);
                normals[i] = new Vector3(normalData[i * 3], normalData[i * 3 + 1], normalData[i * 3 + 2]);
            }

            indices = new int[indexData.Length];
            Array.Copy(indexData, indices, indexData.Length);
            
        }

        public Mesh(Vector3[] verexData, Vector3[] normalData, int[] indexData, float error)
        {
            this.error = error;
            if (verexData.Length != normalData.Length)
                throw new ArgumentNullException("Normal buffer is not equials to vertex buffer");

            vertices = verexData;
            normals = normalData;

            indices = new int[indexData.Length];
            Array.Copy(indexData, indices, indexData.Length);

        }

        public void Apply(Matrix4x4 matrix)
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Transform(vertices[i], matrix);
                normals[i] = Vector3.Normalize(Vector3.TransformNormal(normals[i], matrix));
            }
        }
    }
}
