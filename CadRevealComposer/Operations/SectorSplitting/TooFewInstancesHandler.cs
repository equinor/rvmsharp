namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Linq;

/// <summary>
/// If there are too few instances of a template in a sector we assume that the cost of batching it is greater than the reward.
/// The batching is done in Reveal and each type of template in each sector will create a separate batch.
/// This can be avoided by converting to TriangleMesh.
/// </summary>
public class TooFewInstancesHandler
{
    // Minimum number of a instances of a template in a sector, otherwise convert to mesh
    // See spike document for data of what this number means TODO: Update after spike document merge
    private const int NumberOfInstancesThreshold = 10; // Minimum number of instances, otherwise convert to mesh

    public APrimitive[] ConvertInstancesWhenTooFew(APrimitive[] geometries)
    {
        var instances = geometries.Where(g => g is InstancedMesh).GroupBy(i => ((InstancedMesh)i).InstanceId);

        var instanceKeyListToConvert = (
            from instanceGroup in instances
            where instanceGroup.Count() > NumberOfInstancesThreshold
            select instanceGroup.Key
        ).ToList();

        return geometries
            .Select(g =>
            {
                if (g is InstancedMesh instanceMesh && instanceKeyListToConvert.Contains(instanceMesh.InstanceId))
                {
                    return ConvertInstanceToMesh(instanceMesh, instanceMesh);
                }
                return g;
            })
            .ToArray();
    }

    private TriangleMesh ConvertInstanceToMesh(InstancedMesh instanceMesh, APrimitive primitive)
    {
        return new TriangleMesh(
            instanceMesh.TemplateMesh,
            primitive.TreeIndex,
            primitive.Color,
            primitive.AxisAlignedBoundingBox
        );
    }
}
