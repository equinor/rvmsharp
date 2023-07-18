namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Linq;

// Convert instances to mesh to avoid too much batching
// See spike document TODO: Update after spike document merge
public class TooFewInstancesHandler
{
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
