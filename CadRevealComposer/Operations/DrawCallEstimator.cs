namespace CadRevealComposer.Operations
{
    using Primitives;
    using System.Linq;

    public static class DrawCallEstimator
    {
        public static (long EstimatedTriangleCount, int EstimatedDrawCalls) Estimate(APrimitive[] geometry)
        {
            var estimatedTriangleCount = geometry.Select(g =>
            {
                switch (g)
                {
                    case Box box:
                        return 12L;
                    case Circle circle:
                        return 2L;
                    case ClosedCone closedCone:
                    case ClosedCylinder closedCylinder:
                    case ClosedEccentricCone closedEccentricCone:
                    case ClosedEllipsoidSegment closedEllipsoidSegment:
                    case ClosedExtrudedRingSegment closedExtrudedRingSegment:
                    case ClosedGeneralCone closedGeneralCone:
                    case ClosedGeneralCylinder closedGeneralCylinder:
                    case ClosedSphericalSegment closedSphericalSegment:
                    case ClosedTorusSegment closedTorusSegment:
                    case Ellipsoid ellipsoid:
                    case ExtrudedRing extrudedRing:
                    case OpenCone openCone:
                        return 4L;
                    case InstancedMesh instancedMesh:
                        return (long)instancedMesh.TriangleCount;
                    case Nut nut:
                        return 4L; // TODO
                    case OpenCylinder openCylinder:
                    case OpenEccentricCone openEccentricCone:
                    case OpenEllipsoidSegment openEllipsoidSegment:
                    case OpenExtrudedRingSegment openExtrudedRingSegment:
                    case OpenGeneralCone openGeneralCone:
                    case OpenGeneralCylinder openGeneralCylinder:
                    case OpenSphericalSegment openSphericalSegment:
                    case OpenTorusSegment openTorusSegment:
                    case ProtoMesh protoMesh:
                    case ProtoMeshFromFacetGroup protoMeshFromFacetGroup:
                    case ProtoMeshFromPyramid protoMeshFromPyramid:
                    case SolidClosedGeneralCone solidClosedGeneralCone:
                    case SolidClosedGeneralCylinder solidClosedGeneralCylinder:
                    case SolidOpenGeneralCone solidOpenGeneralCone:
                    case SolidOpenGeneralCylinder solidOpenGeneralCylinder:
                    case Sphere sphere:
                    case Torus torus:
                    case Ring ring:
                        return 0L;
                    case TriangleMesh triangleMesh:
                        return (long)triangleMesh.TempTessellatedMesh.Triangles.Count / 3;
                    default:
                        return 0L;
                }
            }).LongCount();

            var typeCount = geometry.Select(g => g.GetType()).Distinct().Count();
            var uniqueTriangleMeshes = geometry.OfType<InstancedMesh>().Select(im => im.TriangleOffset).Distinct().Count();

            var estimatedDrawCallCount = typeCount + uniqueTriangleMeshes;

            return (estimatedTriangleCount, estimatedDrawCallCount);
        }
    }
}