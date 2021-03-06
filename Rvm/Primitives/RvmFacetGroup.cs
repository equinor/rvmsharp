using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmFacetGroup : RvmPrimitive
    {
        public class RvmContour
        {
            public readonly (Vector3 v, Vector3 n)[] _vertices;

            public RvmContour((Vector3 v, Vector3 n)[] vertices)
            {
                _vertices = vertices;
            }
        }

        public class RvmPolygon
        {
            public readonly RvmContour[] _contours;

            public RvmPolygon(RvmContour[] contours)
            {
                _contours = contours;
            }
        }
        
        public readonly RvmPolygon[] _polygons;

        public RvmFacetGroup(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, RvmPolygon[] polygons)
            : base(version, kind, matrix, bBoxLocal)
        {
            _polygons = polygons;
        }
    }
}