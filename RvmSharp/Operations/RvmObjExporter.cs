namespace RvmSharp.Operations;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Containers;
using Exporters;
using Primitives;
using Tessellation;

public static class RvmObjExporter
{
    public static void ExportToObj(
        RvmStore rvmStore,
        float tolerance,
        string outputFilename,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions = null,
        (Action<int> init, Action tick)? tessellationProgressCallback = null,
        (Action<int> init, Action tick)? exportProgressCallback = null,
        bool useTagNaming = false
    )
    {
        (RvmNode node, string name)[] namedLeafs;
        if (useTagNaming)
        {
            namedLeafs = rvmStore.RvmFiles
                .SelectMany(rvm => rvm.Model.Children.SelectMany(n =>
                    CollectGeometryNodesWithTag(n, attributeExclusions, inheritedTag: null)))
                .ToArray();
            DeduplicateNames(namedLeafs);
        }
        else
        {
            namedLeafs = rvmStore.RvmFiles
                .SelectMany(rvm => rvm.Model.Children.SelectMany(n => CollectGeometryNodes(n, attributeExclusions)))
                .Select(n => (n, n.Name))
                .ToArray();
        }

        var totalLeafs = namedLeafs.Length;
        tessellationProgressCallback?.init(totalLeafs);
        var meshes = namedLeafs
            .AsParallel()
            .Select(entry =>
            {
                var tessellatedMeshes = TessellatorBridge.Tessellate(entry.node, tolerance);
                tessellationProgressCallback?.tick();
                return (name: entry.name, primitives: tessellatedMeshes);
            })
            .ToArray();

        var totalMeshes = meshes.Length;
        exportProgressCallback?.init(totalMeshes);

        using var objExporter = new ObjExporter(outputFilename);
        Color? previousColor = null;
        foreach ((string objectName, (RvmMesh, Color)[] primitives) in meshes)
        {
            objExporter.StartObject(objectName);
            objExporter.StartGroup(objectName);

            foreach ((RvmMesh? mesh, Color color) in primitives)
            {
                if (previousColor != color)
                    objExporter.StartMaterial(color);
                objExporter.WriteMesh(mesh);
                previousColor = color;
            }
            exportProgressCallback?.tick();
        }
    }

    /// <summary>
    /// Build a tag-based name: "Tag-Dashed/ExtraInfo-Dashed".
    /// Falls back to the node's own Name if no tag is available.
    /// </summary>
    internal static string ResolveTagName(string? inheritedTag, RvmNode node)
    {
        if (inheritedTag == null)
            return node.Name;

        var tagPart = inheritedTag.Replace(' ', '-');

        // Strip path reference (everything from first '/' onward) from the node name
        var nodeName = node.Name;
        var slashIndex = nodeName.IndexOf('/');
        var extraInfo = slashIndex >= 0 ? nodeName[..slashIndex].TrimEnd() : nodeName;
        extraInfo = extraInfo.Replace(' ', '-');

        return $"{tagPart}/{extraInfo}";
    }

    /// <summary>
    /// Sanitize a RefNo value for use as a name suffix: strip leading '=' and replace '/' with '-'.
    /// </summary>
    private static string SanitizeRefNo(string refNo)
    {
        if (refNo.StartsWith('='))
            refNo = refNo[1..];
        return refNo.Replace('/', '-');
    }

    /// <summary>
    /// For any duplicate names in the array, append "_RefNo" to disambiguate.
    /// Mutates the name field of the tuples in place.
    /// </summary>
    internal static void DeduplicateNames((RvmNode node, string name)[] entries)
    {
        var nameCounts = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            nameCounts.TryGetValue(entry.name, out var count);
            nameCounts[entry.name] = count + 1;
        }

        var duplicateNames = new HashSet<string>(
            nameCounts.Where(kv => kv.Value > 1).Select(kv => kv.Key));

        if (duplicateNames.Count == 0)
            return;

        for (var i = 0; i < entries.Length; i++)
        {
            if (!duplicateNames.Contains(entries[i].name))
                continue;

            if (entries[i].node.Attributes.TryGetValue("RefNo", out var refNo))
                entries[i].name = $"{entries[i].name}_{SanitizeRefNo(refNo)}";
        }
    }

    private static IEnumerable<(RvmNode node, string name)> CollectGeometryNodesWithTag(
        RvmNode root,
        IReadOnlyList<(string Key, Regex ValuePattern)>? exclusions,
        string? inheritedTag)
    {
        if (exclusions != null)
        {
            foreach (var (key, pattern) in exclusions)
            {
                if (root.Attributes.TryGetValue(key, out var val) && pattern.IsMatch(val))
                    yield break;
            }
        }

        var currentTag = root.Attributes.TryGetValue("Tag", out var tag) ? tag : inheritedTag;

        if (root.Children.OfType<RvmPrimitive>().Any())
            yield return (root, ResolveTagName(currentTag, root));

        foreach (var entry in root.Children.OfType<RvmNode>()
                     .SelectMany(n => CollectGeometryNodesWithTag(n, exclusions, currentTag)))
            yield return entry;
    }

    private static IEnumerable<RvmNode> CollectGeometryNodes(
        RvmNode root,
        IReadOnlyList<(string Key, Regex ValuePattern)>? exclusions)
    {
        if (exclusions != null)
        {
            foreach (var (key, pattern) in exclusions)
            {
                if (root.Attributes.TryGetValue(key, out var val) && pattern.IsMatch(val))
                    yield break;
            }
        }

        if (root.Children.OfType<RvmPrimitive>().Any())
            yield return root;
        foreach (var geometryNode in root.Children.OfType<RvmNode>()
                     .SelectMany(n => CollectGeometryNodes(n, exclusions)))
            yield return geometryNode;
    }
}
