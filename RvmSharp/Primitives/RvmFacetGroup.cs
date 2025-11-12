namespace RvmSharp.Primitives;

using System.Linq;
using System.Numerics;

public record RvmFacetGroup(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    RvmFacetGroup.RvmPolygon[] Polygons
) : RvmPrimitive(Version, RvmPrimitiveKind.FacetGroup, Matrix, BoundingBoxLocal)
{
    public record RvmContour((Vector3 Vertex, Vector3 Normal)[] Vertices);

    public record RvmPolygon(RvmContour[] Contours);

    public RvmBoundingBox CalculateBoundingBoxFromVertexPositions()
    {
        var max = Polygons
            .SelectMany(x => x.Contours)
            .SelectMany(c => c.Vertices)
            .Select(vn => vn.Vertex)
            .Aggregate(new Vector3(float.MinValue, float.MinValue, float.MinValue), Vector3.Max);
        var min = Polygons
            .SelectMany(x => x.Contours)
            .SelectMany(c => c.Vertices)
            .Select(vn => vn.Vertex)
            .Aggregate(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), Vector3.Min);
        return new RvmBoundingBox(min, max);
    }
}
