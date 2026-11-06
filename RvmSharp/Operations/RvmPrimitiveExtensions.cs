namespace RvmSharp.Operations;

using System;
using System.Numerics;
using Primitives;

public static class RvmPrimitiveExtensions
{
    /// <summary>
    /// Copies and transforms vertex positions and normals into new buffers without changing the source geometry.
    /// Transformed normals are normalized to unit length; zero normals remain zero.
    /// </summary>
    /// <param name="group">The source facet group.</param>
    /// <param name="matrix">The vertex transform; normals use its inverse transpose.</param>
    public static RvmFacetGroup TransformVertexData(this RvmFacetGroup group, Matrix4x4 matrix)
    {
        // The inverse transpose keeps normals perpendicular to the surface under non-uniform scaling.
        if (!Matrix4x4.Invert(matrix, out var matrixInverted))
        {
            throw new ArgumentException("Matrix cannot be inverted, to adjust normals we need to invert input matrix");
        }
        var matrixInvertedTransposed = Matrix4x4.Transpose(matrixInverted);
        // Count first so the transformed group needs only three exactly sized arrays.
        // Copying the views alone would still alias the original geometry; rebuild them over fresh buffers.
        var contourCount = 0;
        var vertexCount = 0;
        foreach (var polygon in group.Polygons)
        {
            contourCount = checked(contourCount + polygon.Contours.Count);
            foreach (var contour in polygon.Contours)
                vertexCount = checked(vertexCount + contour.Vertices.Count);
        }

        var vertices = new (Vector3 Vertex, Vector3 Normal)[vertexCount];
        var contours = new RvmFacetGroup.RvmContour[contourCount];
        var polygons = new RvmFacetGroup.RvmPolygon[group.Polygons.Length];
        var contourIndex = 0;
        var vertexIndex = 0;
        for (var polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++)
        {
            var polygon = group.Polygons[polygonIndex];
            var firstContour = contourIndex;
            foreach (var contour in polygon.Contours)
            {
                var firstVertex = vertexIndex;
                foreach (var (vertex, normal) in contour.Vertices)
                {
                    var transformedNormal = Vector3.TransformNormal(normal, matrixInvertedTransposed);
                    // Scaling changes normal lengths; restore unit length without turning zero normals into NaN.
                    vertices[vertexIndex++] = (
                        Vector3.Transform(vertex, matrix),
                        transformedNormal == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(transformedNormal)
                    );
                }

                contours[contourIndex++] = new RvmFacetGroup.RvmContour(
                    new ArraySegment<(Vector3 Vertex, Vector3 Normal)>(vertices, firstVertex, contour.Vertices.Count)
                );
            }

            polygons[polygonIndex] = new RvmFacetGroup.RvmPolygon(
                new ArraySegment<RvmFacetGroup.RvmContour>(contours, firstContour, polygon.Contours.Count)
            );
        }

        return group with
        {
            Polygons = polygons,
        };
    }
}
