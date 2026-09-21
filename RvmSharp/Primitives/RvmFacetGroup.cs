namespace RvmSharp.Primitives;

using System;
using System.Numerics;

public record RvmFacetGroup(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    RvmFacetGroup.RvmPolygon[] Polygons
) : RvmPrimitive(Version, RvmPrimitiveKind.FacetGroup, Matrix, BoundingBoxLocal)
{
    // Value-type views keep the polygon/contour hierarchy without a heap object or array per contour.
    // The parser packs their contents into shared buffers. Readonly applies to the views, not the arrays:
    // treat their contents as read-only, and allocate new buffers when transforming geometry.
    public readonly record struct RvmContour(ArraySegment<(Vector3 Vertex, Vector3 Normal)> Vertices);

    public readonly record struct RvmPolygon(ArraySegment<RvmContour> Contours);

    /// <summary>
    /// Calculates a (local) bounding box that encapsulates all vertex positions in this facet group.
    /// </summary>
    /// <remarks> Will likely no longer be needed after RVM export is fixed to write correct bounding boxes. </remarks>
    /// <returns>An axis aligned bounding box in local space based on actual vertex coordinates</returns>
    public RvmBoundingBox CalculateBoundingBoxFromVertexPositions()
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var polygon in Polygons)
        {
            foreach (var contour in polygon.Contours)
            {
                foreach (var (vertex, _) in contour.Vertices)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
            }
        }
        return new RvmBoundingBox(min, max);
    }
}
