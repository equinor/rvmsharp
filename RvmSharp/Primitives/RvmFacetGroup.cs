namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmFacetGroup : RvmPrimitive
    {
        public class RvmContour
        {
            public readonly (Vector3 v, Vector3 n)[] Vertices;

            public RvmContour((Vector3 v, Vector3 n)[] vertices)
            {
                Vertices = vertices;
            }
        }

        public class RvmPolygon
        {
            public readonly RvmContour[] Contours;

            public RvmPolygon(RvmContour[] contours)
            {
                Contours = contours;
            }
        }
        
        public readonly RvmPolygon[] Polygons;

        public RvmFacetGroup(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, RvmPolygon[] polygons)
            : base(version, kind, matrix, bBoxLocal)
        {
            Polygons = polygons;
        }
    }
}