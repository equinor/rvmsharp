namespace CadRevealComposer.Operations
{
    using Primitives;
    using System;
    using System.Linq;

    public static class DrawCallEstimator
    {
        private enum RenderPrimitive
        {
            Box,
            Circle,
            Cone,
            EccentricCone,
            EllipsoidSegment,
            Rectangle,
            RingSegment,
            SphericalSegment,
            TorusSegment,
            Nut,
            SlopedCylinder
        }

        private static int GetTriangleCount(this RenderPrimitive primitive)
        {
            switch (primitive)
            {
                case RenderPrimitive.Box:
                    return 12;
                case RenderPrimitive.Circle:
                case RenderPrimitive.Rectangle:
                case RenderPrimitive.RingSegment:
                    return 2;
                case RenderPrimitive.Cone:
                case RenderPrimitive.EccentricCone:
                case RenderPrimitive.EllipsoidSegment:
                case RenderPrimitive.SphericalSegment:
                case RenderPrimitive.SlopedCylinder:
                    return 4;
                case RenderPrimitive.TorusSegment:
                    return 120;
                case RenderPrimitive.Nut:
                    return 24;
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }

        private static RenderPrimitive[] GetRenderPrimitives(this APrimitive primitive)
        {
            return primitive switch
            {
                Box => new[] { RenderPrimitive.Box },
                Circle => new [] { RenderPrimitive.Circle },
                ClosedCone => new [] { RenderPrimitive.Circle, RenderPrimitive.Circle, RenderPrimitive.Cone }, // TODO: if one R is 0, should be 1 circle
                ClosedCylinder => new [] { RenderPrimitive.Circle, RenderPrimitive.Circle, RenderPrimitive.Cone },
                ClosedEccentricCone => new [] { RenderPrimitive.Circle, RenderPrimitive.Circle, RenderPrimitive.EccentricCone },
                ClosedEllipsoidSegment => new [] { RenderPrimitive.Circle, RenderPrimitive.EllipsoidSegment },
                ClosedExtrudedRingSegment => new [] { RenderPrimitive.Rectangle, RenderPrimitive.Rectangle, RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.Cone, RenderPrimitive.Cone },
                ClosedGeneralCone => new [] { RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.SlopedCylinder }, // TODO: this one is unsure
                ClosedGeneralCylinder => new [] { RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.SlopedCylinder },
                ClosedSphericalSegment => new [] { RenderPrimitive.Circle, RenderPrimitive.SphericalSegment},
                ClosedTorusSegment => new [] { RenderPrimitive.TorusSegment },
                Ellipsoid => new [] { RenderPrimitive.EllipsoidSegment },
                ExtrudedRing => new[] {RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.Cone, RenderPrimitive.Cone },
                Nut => new[] { RenderPrimitive.Nut },
                OpenCone => new[] { RenderPrimitive.Cone },
                OpenCylinder => new[] { RenderPrimitive.Cone },
                OpenEccentricCone => new[] {RenderPrimitive.EccentricCone },
                OpenEllipsoidSegment => new[] { RenderPrimitive.EllipsoidSegment },
                OpenExtrudedRingSegment => new[] {RenderPrimitive.Cone, RenderPrimitive.Cone },
                OpenGeneralCone => new[] { RenderPrimitive.SlopedCylinder},
                OpenGeneralCylinder => new[] { RenderPrimitive.SlopedCylinder},
                OpenSphericalSegment => new[] { RenderPrimitive.SphericalSegment },
                OpenTorusSegment => new[] { RenderPrimitive.TorusSegment },
                Ring => new[] { RenderPrimitive.RingSegment },
                SolidClosedGeneralCone => new[] {RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.SlopedCylinder, RenderPrimitive.SlopedCylinder  },
                SolidClosedGeneralCylinder => new[] {RenderPrimitive.RingSegment, RenderPrimitive.RingSegment, RenderPrimitive.SlopedCylinder , RenderPrimitive.SlopedCylinder },
                SolidOpenGeneralCone => new[] {RenderPrimitive.SlopedCylinder, RenderPrimitive.SlopedCylinder },
                SolidOpenGeneralCylinder => new[] { RenderPrimitive.SlopedCylinder, RenderPrimitive.SlopedCylinder},
                Sphere => new[] { RenderPrimitive.SphericalSegment },
                Torus => new[] { RenderPrimitive.TorusSegment },
                _ => Array.Empty<RenderPrimitive>()
            };
        }

        public static (long EstimatedTriangleCount, int EstimatedDrawCalls) Estimate(APrimitive[] geometry)
        {
            var renderPrimitives = geometry.SelectMany(g => g.GetRenderPrimitives()).ToArray();
            var estimatedPrimitiveDrawCallCount = renderPrimitives.Distinct().Count();
            var estimatedPrimitiveTriangleCount = renderPrimitives.Select(p => (long)p.GetTriangleCount()).Sum();

            var estimatedTriangleMeshTriangleCount = geometry.OfType<TriangleMesh>()
                .Select(tm => (long)tm.TempTessellatedMesh!.Triangles.Count / 3).Sum();
            var estimatedTriangleMeshDrawCallCount = estimatedTriangleMeshTriangleCount > 0 ? 1 : 0;

            var instancedMeshes = geometry.OfType<InstancedMesh>().Select(im => im.TempTessellatedMesh).ToArray();
            var estimatedInstancedMeshTriangleCount = instancedMeshes.Select(im => (long)im!.Triangles.Count / 3).Sum();
            var estimatedInstancedMeshDrawCallCount = instancedMeshes.Distinct().Count();

            var estimatedTriangleCount = estimatedPrimitiveTriangleCount + estimatedTriangleMeshTriangleCount +
                                         estimatedInstancedMeshTriangleCount;
            var estimatedDrawCallCount = estimatedPrimitiveDrawCallCount + estimatedTriangleMeshDrawCallCount +
                                         estimatedInstancedMeshDrawCallCount;

            return (estimatedTriangleCount, estimatedDrawCallCount);
        }
    }
}