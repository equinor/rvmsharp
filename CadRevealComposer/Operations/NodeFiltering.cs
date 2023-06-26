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
                regexFilters,
                treeIndexGenerator
            );
            if (rootFiltered != null)
                list.AddRange(CadRevealNode.GetAllNodesFlat(rootFiltered));
        }
        return list;
    }

    public static CadRevealNode? TraverseFilter(
        CadRevealNode root,
        CadRevealNode? parent,
        Regex[] filters,
        TreeIndexGenerator treeIndexGenerator
    )
    {
        // TODO: Make this a real glob filter?
        if (filters.Any(filter => filter.IsMatch(root.Name)))
            return null;
        // Remap the treeindex hierarchy
        var newTreeIndex = treeIndexGenerator.GetNextId();
        var newRoot = root with { TreeIndex = newTreeIndex, Parent = parent };
        var children = new List<CadRevealNode>();
        foreach (CadRevealNode child in root.Children ?? ArraySegment<CadRevealNode>.Empty)
        {
            var childFiltered = TraverseFilter(child, newRoot, filters, treeIndexGenerator);
            if (childFiltered != null)
                children.Add(childFiltered);
        }

        newRoot.Children = children.ToArray();
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
