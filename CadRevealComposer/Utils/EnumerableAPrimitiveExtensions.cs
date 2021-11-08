namespace CadRevealComposer.Utils
{
    using Primitives;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public static class EnumerableAPrimitiveExtensions
    {
        public static Vector3 GetBoundingBoxMin(this IEnumerable<APrimitive> primitives)
        {
            return primitives.Select(p => p.AxisAlignedBoundingBox.Min).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        }

        public static Vector3 GetBoundingBoxMax(this IEnumerable<APrimitive> primitives)
        {
            return primitives.Select(p => p.AxisAlignedBoundingBox.Max).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        }
    }
}