namespace RvmSharp.Primitives;

using Containers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public record RvmNode(uint Version, string Name, Vector3 Translation, uint MaterialId) : RvmGroup(Version)
{
    // ReSharper disable once CollectionNeverQueried.Global
    public readonly Dictionary<string, string> Attributes = new();
    public readonly List<RvmGroup> Children = new List<RvmGroup>();

    internal void AddChild(RvmGroup rvmGroup)
    {
        Children.Add(rvmGroup);
    }

    public static IEnumerable<RvmPrimitive> GetAllPrimitivesFlat(RvmNode root)
    {
        foreach (var child in root.Children.OfType<RvmPrimitive>())
        {
            yield return child;
        }
        foreach (var rvmNode in root.Children.OfType<RvmNode>())
        {
            var primitives = GetAllPrimitivesFlat(rvmNode);
            foreach (var primitive in primitives)
            {
                yield return primitive;
            }
        }
    }
}
