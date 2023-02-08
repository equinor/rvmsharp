namespace CadRevealComposer.Utils;

using Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class EnumerableAPrimitiveExtensions
{
    public static BoundingBox CalculateBoundingBox(this IReadOnlyCollection<APrimitive> primitives)
    {
        // It should be possible to do this in just one pass of the array. Profile to see if its worth it.
        var min = primitives.Select(p => p.AxisAlignedBoundingBox.Min).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        var max = primitives.Select(p => p.AxisAlignedBoundingBox.Max).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        return new BoundingBox(min, max);
    }
}