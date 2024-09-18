namespace CadRevealComposer.Operations.SectorSplitting;

using System.Collections.Generic;
using System.Linq;
using Primitives;
using Tessellating;

public class TooFewPrimitivesHandler
{
    private const int NumberOfPrimitivesThreshold = 10; // Arbitrary number

    public int TotalGroupsOfPrimitive { get; private set; }
    public int TriedConvertedGroupsOfPrimitives { get; private set; }
    public int SuccessfullyConvertedGroupsOfPrimitives { get; private set; }
    public int AdditionalNumberOfTriangles { get; private set; }

    public APrimitive[] ConvertPrimitivesWhenTooFew(APrimitive[] geometries)
    {
        var newGeometries = new List<APrimitive>();
        var primitiveGroups = geometries.GroupBy(x => x.GetType());

        foreach (var group in primitiveGroups)
        {
            if (group.Key == typeof(TriangleMesh) || group.Key == typeof(InstancedMesh))
            {
                newGeometries.AddRange(group);
                continue;
            }

            TotalGroupsOfPrimitive++;
            if (group.Count() < NumberOfPrimitivesThreshold)
            {
                TriedConvertedGroupsOfPrimitives++;

                var convertedGeometries = group
                    .Select(primitive => APrimitiveTessellator.TryToTessellate(primitive) ?? primitive)
                    .ToArray();

                if (convertedGeometries.Any(x => x is TriangleMesh)) // Can be false if primitives aren't handled, currently: EllipsoidSegment and GeneralCylinder
                {
                    SuccessfullyConvertedGroupsOfPrimitives++;
                    foreach (var convertedGeometry in convertedGeometries)
                    {
                        if (convertedGeometry is TriangleMesh convertedTriangleMesh)
                            AdditionalNumberOfTriangles += convertedTriangleMesh.Mesh.TriangleCount;
                    }
                }

                newGeometries.AddRange(convertedGeometries);
            }
            else
            {
                newGeometries.AddRange(group);
            }
        }

        return newGeometries.ToArray();
    }
}
