namespace CadRevealComposer.Utils;

using Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class EnumerableAPrimitiveExtensions
{
    public static BoundingBox? CalculateBoundingBox(this IReadOnlyCollection<APrimitive> primitives)
    {
        if (primitives.Count == 0)
        {
            return null;
        }
        var vectorMinVal = new Vector3(float.MinValue);
        var vectorMaxVal = new Vector3(float.MaxValue);
        // It should be possible to do this in just one pass of the array. Profile to see if its worth it.
        var min = primitives.Select(p => p.AxisAlignedBoundingBox.Min).Aggregate(vectorMaxVal, Vector3.Min);
        var max = primitives.Select(p => p.AxisAlignedBoundingBox.Max).Aggregate(vectorMinVal, Vector3.Max);
        if (min == vectorMaxVal && max == vectorMinVal)
        {
            return null;
        }
        return new BoundingBox(min, max);
    }
}
