namespace RvmSharp.Containers;

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
        AttachAttributes(pdms, Model.Children);
    }

    /// <summary>
    /// Performance oriented version of AttachAttributes which may reduce memory allocations, memory usage and CPU processing time.
    /// </summary>
    /// <param name="txtFilename">File path RVM TEXT</param>
    /// <param name="attributesToExclude">Exclude node attributes by name (case sensitive). If a attribute is not needed this can help to avoid string memory allocations and reduce processing time.</param>
    /// <param name="stringInternPool">String intern pool to deduplicate string allocations and reuse string instances.</param>
    public void AttachAttributes(string txtFilename, IReadOnlyList<string> attributesToExclude,
        IStringInternPool? stringInternPool)
    {
        var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename, attributesToExclude, stringInternPool);
        AttachAttributes(pdms, Model.Children);
    }



    public static void AttachAttributes(
        IReadOnlyList<PdmsTextParser.PdmsNode> attributeNodes,
        IReadOnlyList<RvmNode> groups)
    {
        if (!attributeNodes.Any())
        {
            // Not all RvmNodes have attributes. If there are no PdmsNodes we just skip the assigning. This works around an issue where the
            // rvm file can contain multiple nodes with the same name if it has been converted from another file format to rvm first.
            return;
        }

        var rvmNodeNameLookup = ToNameLookupDiscardNodesWithDuplicateNames(groups);

        foreach (var attributeNode in attributeNodes)
        {
            if (!rvmNodeNameLookup.TryGetValue(attributeNode.Name, out var rvmNode) || rvmNode == null)
                continue; // Ignore nodes that were not found, or which did not have a Unique name

            foreach (var kvp in attributeNode.MetadataDict)
                rvmNode.Attributes.Add(kvp.Key, kvp.Value);
            AttachAttributes(attributeNode.Children, rvmNode.Children.OfType<RvmNode>().ToArray());
        }
    }

    /// <summary>
    /// This method indexes the input array, but will `null` the return value for duplicated names.
    /// RVM should in theory not support duplicates, and much less support attributes for these files.
    ///
    /// This decision may be wrong, so if any issues occur due to this filtering we must rewrite.
    /// </summary>
    private static Dictionary<string, RvmNode?> ToNameLookupDiscardNodesWithDuplicateNames(IReadOnlyList<RvmNode> a)
    {
        var dict = new Dictionary<string, RvmNode?>();
        foreach (RvmNode rvmNode in a)
        {
            if(string.IsNullOrEmpty(rvmNode.Name))
                continue;

            if (dict.ContainsKey(rvmNode.Name))
            {
                dict[rvmNode.Name] = null; // Discard nodes that have duplicate names.
            }
            else
            {
                dict.Add(rvmNode.Name, rvmNode);
            }
        }

        return dict;
    }
}