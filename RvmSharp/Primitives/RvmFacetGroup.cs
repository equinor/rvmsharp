namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmFacetGroup(
        uint Version,
        Matrix4x4 Matrix,
        RvmBoundingBox BoundingBoxLocal,
        RvmFacetGroup.RvmPolygon[] Polygons) 
    : RvmPrimitive(Version, RvmPrimitiveKind.FacetGroup, Matrix, BoundingBoxLocal)
{
    public record RvmContour((Vector3 Vertex, Vector3 Normal)[] Vertices);

    public record RvmPolygon(RvmContour[] Contours);
}