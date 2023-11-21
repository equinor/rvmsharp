namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Linq;

/// <summary>
/// If there are too few instances of a template in a sector we assume that the cost of batching it is greater than the reward.
/// The batching is done in Reveal and each type of template in each sector will create a separate batch.
/// This can be avoided by converting to TriangleMesh.
/// </summary>
public class TooFewInstancesHandler
{
    public APrimitive[] ConvertInstancesWhenTooFew(APrimitive[] geometries)
    {
        var instanceGroups = geometries.Where(g => g is InstancedMesh).GroupBy(i => ((InstancedMesh)i).InstanceId);

        var instanceKeyListToConvert = instanceGroups.Where(ShouldConvert).Select(g => g.Key).ToArray();

        return geometries
            .Select(g =>
            {
                if (g is InstancedMesh instanceMesh && instanceKeyListToConvert.Contains(instanceMesh.InstanceId))
                {
                    return ConvertInstanceToMesh(instanceMesh);
                }
                return g;
            })
            .ToArray();
    }

    private TriangleMesh ConvertInstanceToMesh(InstancedMesh instanceMesh)
    {
        var templateMesh = instanceMesh.TemplateMesh;

        var newMesh = templateMesh.Clone();
        newMesh.Apply(instanceMesh.InstanceMatrix);

        return new TriangleMesh(
            newMesh,
            instanceMesh.TreeIndex,
            instanceMesh.Color,
            instanceMesh.AxisAlignedBoundingBox,
            instanceMesh.Area
        );
    }

    /// <summary>
    /// Uses the curve y = a^2 / (x - b) + c to check if the instance group should be converted or not
    ///
    /// a = Curve steepness, higher values gives a more rounded "turn". The Curve will pass through the point (a, a)
    ///
    /// X-axis: Number of total triangles in the instance group
    /// Y-axis: Number of instances
    ///
    /// Only the first quadrant part of the curve is relevant, so everything to the left of the Y asymptote is set to be converted
    ///
    /// Use, for instance, WolframAlpha or Geogebra to plot the curve and points for tweaking
    /// </summary>
    /// <param name="instanceGroup"></param>
    /// <returns></returns>
    private bool ShouldConvert(IGrouping<ulong, APrimitive> instanceGroup)
    {
        int numberOfInstancesThreshold = 100; // Always keep when the number of instances is exceeding the threshold
        int numberOfTrianglesThreshold = 10000; // Alwyas keep when the number of triangles is exceeding the threshold

        float a = 100; // Steepness

        if (a < 0)
            throw new ArgumentException($"The value of A needs to be larger than zero. It was: {a}");

        int numberOfInstances = instanceGroup.Count();
        int numberOfTriangles = ((InstancedMesh)instanceGroup.First()).TemplateMesh.TriangleCount * numberOfInstances;

        if (numberOfInstances > numberOfInstancesThreshold || numberOfTriangles > numberOfTrianglesThreshold)
            return false;

        if (numberOfInstances < (a * a) / numberOfTriangles)
        {
            return true;
        }

        return false;
    }
}
