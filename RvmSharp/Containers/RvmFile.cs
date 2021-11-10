namespace RvmSharp.Containers
{
    using Ben.Collections.Specialized;
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

        public void AttachAttributes(string txtFilename, IInternPool stringInternPool)
        {
            var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename, stringInternPool);
            AssignRecursive(pdms, Model.Children);
        }

        private static void AssignRecursive(IReadOnlyList<PdmsTextParser.PdmsNode> attributeNodes,
            IReadOnlyList<RvmNode> groups)
        {
            //if (attributes.Count != groups.Count)
            //    Console.Error.WriteLine("Length of attribute nodes does not match group length");
            var rvmNodeNameLookup = groups.ToDictionary(x => x.Name, y => y);

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
}