using Equinor.MeshOptimizationPipeline;
using Mop.Hierarchy.Model;
using System.Collections.Generic;

namespace Mop.Hierarchy.Extensions
{
    using CadRevealComposer;

    public static class KeyExtensions
    {
        public static string GetKey(this BoundingBox aabb)
        {
            return $"{aabb.min.x.ToString("0.00")},{aabb.min.y.ToString("0.00")},{aabb.min.z.ToString("0.00")}" +
                $",{aabb.max.x.ToString("0.00")},{aabb.max.y.ToString("0.00")},{aabb.max.z.ToString("0.00")}";
        }

        public static string GetKey(this KeyValuePair<string, string> pdmsEntry)
        {
            return $"{pdmsEntry.Key}:{pdmsEntry.Value}";
        }

        public static AABB ToAABB(this AABBSerializable input, int id)
        {
            return new AABB
            {
                Id = id,
                min = new Vector3f { x = input.min.x, y = input.min.y, z = input.min.z },
                max = new Vector3f { x = input.max.x, y = input.max.y, z = input.max.z }
            };
        }
    }
}