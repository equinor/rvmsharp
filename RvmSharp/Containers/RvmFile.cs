namespace RvmSharp.Containers
{
    using Primitives;
    using System.Collections.Generic;
    using System.Linq;

    public class RvmFile
    {
        public readonly uint Version;
        public readonly string Info;
        public readonly string Note;
        public readonly string Date;
        public readonly string User;
        public readonly string Encoding;

        public RvmModel Model { get; internal set; }

        public RvmFile(uint version, string info, string note, string date, string user, string encoding)
        {
            Version = version;
            Info = info;
            Note = note;
            Date = date;
            User = user;
            Encoding = encoding;
        }

        public void AttachAttributes(string txtFilename)
        {
            var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename);
            AssignRecursive(pdms, Model.children);
        }
        
        private static void AssignRecursive(IList<PdmsTextParser.PdmsNode> attributes, IList<RvmNode> groups)
        {
            //if (attributes.Count != groups.Count)
            //    Console.Error.WriteLine("Length of attribute nodes does not match group length");
            var copy = new List<RvmNode>(groups);
            for (var i = 0; i < attributes.Count; i++)
            {
                var pdms = attributes[i];
                for (var k = 0; k < copy.Count; k++)
                {
                    var group = copy[k];
                    if (group.Name == pdms.Name)
                    {
                        // todo attr
                        foreach (var kvp in pdms.MetadataDict)
                            group.Attributes.Add(kvp.Key, kvp.Value);
                        AssignRecursive(pdms.Children, group.Children.OfType<RvmNode>().ToArray());
                        break;
                    }
                }
            }
        }
    }
}