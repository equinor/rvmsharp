namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Collections.Generic;
using System.Linq;
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
            if (group.FirstOrDefault() is TriangleMesh or InstancedMesh)
            {
                newGeometries.AddRange(group);
                continue;
            }

            TotalGroupsOfPrimitive++;
            if (group.Count() < NumberOfPrimitivesThreshold)
            {
                TriedConvertedGroupsOfPrimitives++;

                var convertedGeometries = group.SelectMany(APrimitiveTessellator.TryToTessellate).ToArray();

                if (convertedGeometries.First() is TriangleMesh) // Can be false if primitives aren't handled, currently: EllipsoidSegment and GeneralCylinder
                {
                    SuccessfullyConvertedGroupsOfPrimitives++;
                    foreach (var convertedGeometry in convertedGeometries)
                    {
                        AdditionalNumberOfTriangles += ((TriangleMesh)convertedGeometry).Mesh.TriangleCount;
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
