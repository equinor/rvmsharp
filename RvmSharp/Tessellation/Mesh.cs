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
        
        public Vector3[] VertexColors;

        public Mesh(float[] vertexData, float[] normalData, int[] triangleData, float error)
        {
            Error = error;
            if (vertexData.Length != normalData.Length)
                throw new ArgumentException("Vertex and normal arrays must have equal length");

            Vertices = new Vector3[vertexData.Length / 3];
            Normals = new Vector3[normalData.Length / 3];
            VertexColors = Array.Empty<Vector3>();

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
            VertexColors = Array.Empty<Vector3>();
        }

        private Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles, Vector3[] vertexColors, float error)
        {
            if (vertices.Length != normals.Length)
                throw new ArgumentException("Vertex and normal arrays must have equal length");

            if (vertices.Length != vertexColors.Length)
                throw new ArgumentException("Vertex and vertex color arrays must have equal length");

            Error = error;
            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
            VertexColors = vertexColors;
        }

        public void Apply(Matrix4x4 matrix)
        {
            for (var i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = Vector3.Transform(Vertices[i], matrix);
                Normals[i] = Vector3.Normalize(Vector3.TransformNormal(Normals[i], matrix));
            }
        }

        public void ApplySingleColor(int value)
        {
            if (value > 0xffffff)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Value must fit in 24 bits, max value is {0xffffff}");
            }
            
            var x = (value & 0xff0000) >> 16;
            var y = (value & 0x00ff00) >> 8;
            var z = (value & 0x0000ff);
            var xf = (x + 0.1f) / 255f;    /////
            var yf = (y + 0.1f) / 255f;    // Add 0.1 to avoid floor rounding errors in float to int conversion in Unreal
            var zf = (z + 0.1f) / 255f;    /////
            var color = new Vector3(xf, yf, zf);
            VertexColors = new Vector3[Vertices.Length];
            Array.Fill(VertexColors, color);
        }

        public static Mesh Merge(Mesh mesh1, Mesh mesh2)
        {
            var mesh1VertexCount = mesh1.Vertices.Length;
            var vertices = mesh1.Vertices.Concat(mesh2.Vertices).ToArray();
            var normals = mesh1.Normals.Concat(mesh2.Normals).ToArray();
            var triangles = mesh1.Triangles.Concat(mesh2.Triangles.Select(t => t + mesh1VertexCount)).ToArray();
            var vertexColors = mesh1.VertexColors.Concat(mesh2.VertexColors).ToArray();
            var error = Math.Max(mesh1.Error, mesh2.Error);
            return vertexColors.Length > 0 ? new Mesh(vertices, normals, triangles, vertexColors, error) : new Mesh(vertices, normals, triangles, error);
        }
    }
}
