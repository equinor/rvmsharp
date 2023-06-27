namespace CadRevealComposer.Operations;

using IdProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class NodeFiltering
{
    /// <summary>
    /// Removes nodes and all children when a node matches a given pattern
    /// Looks at the node name only, and not its path.
    ///
    /// Returns a new list of filtered nodes, where the children have been removed, and the treeIndexes remapped????
    /// </summary>
    public static List<CadRevealNode> FilterAndReindexNodesByGlobs(
        IEnumerable<CadRevealNode> nodes,
        string[] globFilters,
        TreeIndexGenerator treeIndexGenerator
    )
    {
        var regexFilters = globFilters.Select(ConvertGlobToRegex).ToArray();
        var roots = nodes.Where(x => x.Parent == null);
        List<CadRevealNode> list = new();
        foreach (CadRevealNode root in roots)
        {
            var rootFiltered = TraverseFilter(
                root,
                parent: null, /*Roots have no parent*/
                regexFilters
            );
            if (rootFiltered != null)
            {
                var withTreeIndexes = TraverseAddTreeIndexes(rootFiltered, null, treeIndexGenerator);
                list.AddRange(CadRevealNode.GetAllNodesFlat(withTreeIndexes));
            }
        }

        return list;
    }

    public static CadRevealNode TraverseAddTreeIndexes(
        CadRevealNode root,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator
    )
    {
        // Remap the treeindex hierarchy
        var newTreeIndex = treeIndexGenerator.GetNextId();
        var newRoot = root with
        {
            TreeIndex = newTreeIndex,
            Parent = parent,
            Geometries = root.Geometries.Select(x => x with { TreeIndex = newTreeIndex }).ToArray()
        };
        List<CadRevealNode> newChildren = new();
        foreach (CadRevealNode child in root.Children ?? ArraySegment<CadRevealNode>.Empty)
        {
            var childFiltered = TraverseAddTreeIndexes(child, newRoot, treeIndexGenerator);
            newChildren.Add(childFiltered);
        }
        // Mutating so the "Parent" is correct for the children...
        newRoot.Children = newChildren.ToArray();
        return newRoot;
    }

    public static CadRevealNode? TraverseFilter(CadRevealNode root, CadRevealNode? parent, Regex[] filters)
    {
        if (filters.Any(filter => filter.IsMatch(root.Name)))
            return null;

        // Remap the treeindex hierarchy
        var newRoot = root with
        {
            Parent = parent,
        };

        var newChildren = new List<CadRevealNode>();
        foreach (CadRevealNode child in root.Children ?? ArraySegment<CadRevealNode>.Empty)
        {
            var childFiltered = TraverseFilter(child, newRoot, filters);
            if (childFiltered != null)
                newChildren.Add(childFiltered);
        }

        newRoot.Children = newChildren.ToArray();
        return newRoot;
    }

    private static Regex ConvertGlobToRegex(string glob)
    {
        // Naive glob implementation inspired from https://stackoverflow.com/a/4146349
        return new Regex(
            "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );
    }
}
