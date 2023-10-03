namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Collections.Generic;
using System.Linq;
using Tessellating;

public class TooFewPrimitivesHandler
{
    private const int NumberOfPrimitivesThreshold = 10;

    public APrimitive[] ConvertPrimitivesWhenTooFew(APrimitive[] geometries)
    {
        var newGeometries = new List<APrimitive>();
        var primitiveGroups = geometries.GroupBy(x => x.GetType());

        foreach (var group in primitiveGroups)
        {
            if (group.Count() < NumberOfPrimitivesThreshold)
            {
                var convertedGeometries = group.SelectMany(APrimitiveTessellator.TryToTessellate).ToArray();
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
