using System;
using System.Linq;
using System.Numerics;
using CadRevealComposer.Primitives;

namespace CadRevealComposer.Operations.SectorSplitting;

/// <summary>
/// Computes a visual importance metric for primitives and nodes.
/// Uses oriented surface area for parametric primitives (Cone, EccentricCone, GeneralCylinder, Box)
/// and AABB surface area for meshes and flat primitives.
/// The final importance value is √(totalSurfaceArea), giving meter-scale units comparable to diagonal.
/// </summary>
public static class VisualImportanceCalculator
{
    /// <summary>
    /// Compute visual importance for a node as √(sum of oriented surface areas of its geometries).
    /// This is O(1) per primitive (no mesh vertex iteration).
    /// </summary>
    public static float GetVisualImportance(this Node node)
    {
        var totalSA = node.Geometries.Sum(EstimateOrientedSurfaceArea);
        return MathF.Sqrt(totalSA);
    }

    /// <summary>
    /// Estimate the oriented surface area of a primitive.
    /// For parametric primitives (Cone, EccentricCone, GeneralCylinder, Box) this uses the actual
    /// geometry parameters, making it orientation-independent. For TriangleMesh, InstancedMesh,
    /// and flat primitives, falls back to AABB surface area which is tight for compact meshes
    /// and avoids inflating importance with internal/overlapping triangles.
    /// </summary>
    public static float EstimateOrientedSurfaceArea(APrimitive primitive)
    {
        return primitive switch
        {
            Cone c => CylindricalSurfaceArea(c.CenterA, c.CenterB, c.RadiusA, c.RadiusB, c.ArcAngle),
            EccentricCone c => CylindricalSurfaceArea(c.CenterA, c.CenterB, c.RadiusA, c.RadiusB),
            GeneralCylinder c => CylindricalSurfaceArea(c.CenterA, c.CenterB, c.Radius, c.Radius, c.ArcAngle),
            Box b => OrientedBoxSurfaceArea(b.InstanceMatrix),
            // For TriangleMesh, InstancedMesh, and flat primitives: AABB surface area.
            // Mesh AABBs are tight (vertices fill the volume) and this avoids inflating
            // importance with internal/overlapping triangles in complex meshes.
            _ => primitive.AxisAlignedBoundingBox.SurfaceArea,
        };
    }

    /// <summary>
    /// Surface area of a frustum/cylinder, scaled by arc fraction for partial arcs.
    /// arcAngle defaults to 2π (full revolution) for primitives that don't specify it (e.g. EccentricCone).
    /// </summary>
    private static float CylindricalSurfaceArea(
        Vector3 centerA,
        Vector3 centerB,
        float r1,
        float r2,
        float arcAngle = MathF.PI * 2f
    )
    {
        var length = Vector3.Distance(centerA, centerB);
        var slant = MathF.Sqrt(length * length + (r1 - r2) * (r1 - r2));
        var arcFraction = arcAngle / (MathF.PI * 2f);
        // Lateral surface (scaled by arc fraction) + end caps (full circles regardless of arc)
        var lateral = MathF.PI * (r1 + r2) * slant * arcFraction;
        var endCaps = MathF.PI * r1 * r1 + MathF.PI * r2 * r2;
        return lateral + endCaps;
    }

    /// <summary>
    /// Extract scale from the instance matrix to get the oriented box dimensions,
    /// then compute surface area from those dimensions.
    /// </summary>
    private static float OrientedBoxSurfaceArea(Matrix4x4 instanceMatrix)
    {
        var sx = new Vector3(instanceMatrix.M11, instanceMatrix.M12, instanceMatrix.M13).Length();
        var sy = new Vector3(instanceMatrix.M21, instanceMatrix.M22, instanceMatrix.M23).Length();
        var sz = new Vector3(instanceMatrix.M31, instanceMatrix.M32, instanceMatrix.M33).Length();
        return 2f * (sx * sy + sx * sz + sy * sz);
    }
}
