namespace RvmSharp.Operations;

using Primitives;
using System;
using System.Linq;
using System.Numerics;

public static class RvmPrimitiveExtensions
{
    public static RvmFacetGroup TransformVertexData(this RvmFacetGroup group, Matrix4x4 matrix)
    {
        if (!Matrix4x4.Invert(matrix, out var matrixInverted))
        {
            throw new ArgumentException("Matrix cannot be inverted, to adjust normals we need to invert input matrix");
        }
        var matrixInvertedTransposed = Matrix4x4.Transpose(matrixInverted);
        return group with
        {
            Polygons = group.Polygons.Select(a => a with
                {
                    Contours = a.Contours.Select(c => c with
                    {
                        Vertices = c.Vertices.Select(v => (
                            Vector3.Transform(v.Vertex, matrix),
                            Vector3.Normalize(Vector3.TransformNormal(v.Normal, matrixInvertedTransposed)))).ToArray()
                    }).ToArray()
                }
            ).ToArray()
        };
    }
}