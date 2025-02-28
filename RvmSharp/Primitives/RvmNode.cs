namespace RvmSharp.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Containers;

/// <summary>
/// RvmNode is a node in the RVM file format.
///
/// A node is just a hierarchical grouping of other nodes and primitives, and does not have a direct representation in the 3D model.
/// See <see cref="RvmPrimitive"/> for the actual geometry, usually found in a Children of a Node
/// </summary>
/// <param name="Version">RVM Version. Unknown use</param>
/// <param name="Name">The nodes name in the Hierarchy</param>
/// <param name="Translation">Unknown use.</param>
/// <param name="MaterialId">What material does this Node have? (Color)</param>
public record RvmNode(uint Version, string Name, Vector3 Translation, uint MaterialId) : RvmGroup(Version)
{
    // ReSharper disable once CollectionNeverQueried.Global -- Public API, it is used externally
    public readonly Dictionary<string, string> Attributes = new Dictionary<string, string>();
    public readonly List<RvmGroup> Children = [];

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

    /// <summary>
    /// Enumerates child RvmNodes recursively in depth-first order.
    /// Excludes any RvmPrimitive childs!
    /// </summary>
    /// <param name="includeSelf">Should this Node be included in the Enumeration?</param>
    /// <returns>All the RvmNodes in depth first order</returns>
    public IEnumerable<RvmNode> EnumerateNodesRecursive(bool includeSelf = true)
    {
        if (includeSelf)
            yield return this;
        foreach (var child in Children.OfType<RvmNode>())
        {
            foreach (var childNode in child.EnumerateNodesRecursive(includeSelf: true))
            {
                yield return childNode;
            }
        }
    }

    /// <summary>
    /// Enumerate all children recursively in depth-first order.
    /// </summary>
    /// <param name="includeSelf">Should `this` Node be included in the Enumeration?</param>
    /// <returns>All the RvmGroups in depth first order</returns>
    public IEnumerable<RvmGroup> EnumerateRecursive(bool includeSelf = true)
    {
        if (includeSelf)
            yield return this;

        foreach (var child in Children)
        {
            switch (child)
            {
                case RvmNode rvmNode:
                    foreach (var c in rvmNode.EnumerateRecursive(includeSelf: true))
                    {
                        yield return c;
                    }
                    break;
                case RvmPrimitive rvmPrimitive:
                    yield return rvmPrimitive;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(child.GetType() + " is not supported yet.");
            }
        }
    }
}
