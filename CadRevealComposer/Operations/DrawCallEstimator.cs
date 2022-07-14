namespace CadRevealComposer.Operations;

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
            Cone => new[] { RenderPrimitive.Circle, RenderPrimitive.Circle, RenderPrimitive.Cone }, // TODO: if one R is 0, should be 1 circle
            EccentricCone => new[] { RenderPrimitive.Circle, RenderPrimitive.Circle, RenderPrimitive.EccentricCone },
            EllipsoidSegment => new [] { RenderPrimitive.EllipsoidSegment },
            GeneralCylinder => new[] { RenderPrimitive.SlopedCylinder },
            GeneralRing => new[] { RenderPrimitive.TorusSegment },
            Nut => new[] { RenderPrimitive.Nut },
            Quad => new[] { RenderPrimitive.Rectangle },
            TorusSegment => new[] { RenderPrimitive.TorusSegment },
            Trapezium => new[] { RenderPrimitive.Rectangle },
            _ => Array.Empty<RenderPrimitive>()
        };
    }

    public static (long EstimatedTriangleCount, int EstimatedDrawCalls) Estimate(APrimitive[] geometry)
    {
        if (geometry == null || !geometry.Any())
            return (0, 0);
        var renderPrimitives = geometry.SelectMany(g => g.GetRenderPrimitives()).ToArray();
        var estimatedPrimitiveDrawCallCount = renderPrimitives.Distinct().Count();
        var estimatedPrimitiveTriangleCount = renderPrimitives.Select(p => (long)p.GetTriangleCount()).Sum();

        var estimatedTriangleMeshTriangleCount = geometry.OfType<TriangleMesh>()
            .Select(tm => (long)tm.Mesh.TriangleCount).Sum();
        var estimatedTriangleMeshDrawCallCount = estimatedTriangleMeshTriangleCount > 0
            ? 1
            : 0;

        var instancedMeshes = geometry.OfType<InstancedMesh>().ToArray();
        var estimatedInstancedMeshTriangleCount = instancedMeshes.Select(im => (long)im.Mesh.TriangleCount).Sum();
        var estimatedInstancedMeshDrawCallCount = instancedMeshes.Distinct().Count();

        var estimatedTriangleCount = estimatedPrimitiveTriangleCount + estimatedTriangleMeshTriangleCount +
                                     estimatedInstancedMeshTriangleCount;
        var estimatedDrawCallCount = estimatedPrimitiveDrawCallCount + estimatedTriangleMeshDrawCallCount +
                                     estimatedInstancedMeshDrawCallCount;

        return (estimatedTriangleCount, estimatedDrawCallCount);
    }

    /// <summary>
    /// Based on the knowledge of I3dWriter and ObjExporter.
    /// </summary>
    /// <param name="primitive"></param>
    /// <returns></returns>
    public static long EstimateByteSize(APrimitive primitive)
    {
        return primitive switch
        {
            Box => 11 * sizeof(ulong),
            Circle => 8 * sizeof(ulong),
            Cone => 10 * sizeof(ulong),
            EccentricCone => 11 * sizeof(ulong),
            EllipsoidSegment => 9 * sizeof(ulong),
            GeneralCylinder => 9 * sizeof(ulong),
            GeneralRing => 9 * sizeof(ulong),
            Nut => 10 * sizeof(ulong),
            Quad => 4 * sizeof(ulong),
            TorusSegment => 9 * sizeof(ulong),
            Trapezium => 4 * sizeof(ulong),
            InstancedMesh => 20 * sizeof(ulong),
            TriangleMesh triangleMesh => 10 * sizeof(ulong) + (long)triangleMesh.Mesh.TriangleCount * 6 * sizeof(float),
            _ => throw new NotImplementedException()
        };
    }
}