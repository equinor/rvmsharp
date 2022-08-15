﻿namespace RvmSharp.Containers;

using BatchUtils;
using Primitives;
using System.Collections.Generic;
using System.Linq;

// Public Api
public class RvmFile
{
    // Public Api
    public record RvmHeader(uint Version, string Info, string Note, string Date, string User, string Encoding);

    public RvmHeader Header { get; }
    public RvmModel Model { get; }

    public RvmFile(RvmHeader header, RvmModel model)
    {
        Header = header;
        Model = model;
    }

    public void AttachAttributes(string txtFilename)
    {
        var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename);
        AssignRecursive(pdms, Model.Children);
    }

    /// <summary>
    /// Performance oriented version of AttachAttributes which may reduce memory allocations, memory usage and CPU processing time.
    /// </summary>
    /// <param name="txtFilename">File path RVM TEXT</param>
    /// <param name="attributesToExclude">Exclude node attributes by name (case sensitive). If a attribute is not needed this can help to avoid string memory allocations and reduce processing time.</param>
    /// <param name="stringInternPool">String intern pool to deduplicate string allocations and reuse string instances.</param>
    public void AttachAttributes(string txtFilename, IReadOnlyList<string> attributesToExclude, IStringInternPool? stringInternPool)
    {
        var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename, attributesToExclude, stringInternPool);
        AssignRecursive(pdms, Model.Children);
    }

    private static void AssignRecursive(IReadOnlyList<PdmsTextParser.PdmsNode> attributeNodes,
        IReadOnlyList<RvmNode> groups)
    {
        if (!attributeNodes.Any())
        {
            // Not all RvmNodes have attributes. If there are no PdmsNodes we just skip the assigning. This works around an issue where the
            // rvm file can contain multiple nodes with the same name if it has been converted from another file format to rvm first.
            return;
        }

        var rvmNodeNameLookup = groups
            .Where(x => !string.IsNullOrEmpty(x.Name)) // Ignoring nodes with no name
            .ToDictionary(x => x.Name, y => y);

        foreach (var attributeNode in attributeNodes)
        {
            if (rvmNodeNameLookup.TryGetValue(attributeNode.Name, out var rvmNode))
            {
                foreach (var kvp in attributeNode.MetadataDict)
                    rvmNode.Attributes.Add(kvp.Key, kvp.Value);
                AssignRecursive(attributeNode.Children, rvmNode.Children.OfType<RvmNode>().ToArray());
            }
        }
    }
}