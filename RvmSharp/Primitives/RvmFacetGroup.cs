namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmFacetGroup(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    RvmFacetGroup.RvmPolygon[] Polygons
) : RvmPrimitive(Version, RvmPrimitiveKind.FacetGroup, Matrix, BoundingBoxLocal)
{
    public struct RvmVertex(Vector3 position, Vector3 normal)
    {
        public Vector3 Vertex = position;
        public Vector3 Normal = normal;
    };

    public struct RvmContour(RvmVertex[] vertices)
    {
        public RvmVertex[] Vertices = vertices;
    };

    public struct RvmPolygon(RvmContour[] contours)
    {
        public RvmContour[] Contours = contours;
    }
}
