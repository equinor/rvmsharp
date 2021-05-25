using System;

namespace CadRevealComposer
{
    using RvmSharp.BatchUtils;
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        static TreeIndexGenerator treeIndexGenerator = new TreeIndexGenerator();
        static NodeIdProvider nodeIdGenerator = new NodeIdProvider();

        static void Main(string[] args)
        {
            var workload = Workload.CollectWorkload(new[] {@"d:\Models\hda\rvm20210126\"});
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
            {
                Console.WriteLine(x.fileName);
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            // Project name og project parameters tull from CC
            var rootNode = new CadNode
            {
                NodeId = nodeIdGenerator.GetNodeId(null),
                TreeIndex = treeIndexGenerator.GetNextId(),
                Parent = null,
                Group = null
            };
            rootNode.Children = rvmStore.RvmFiles.SelectMany(f => f.Model.Children).Select(root =>  CollectGeometryNodes(root, rootNode)).ToArray();
            
            // TODO: Nodes must be generated for implicit geometry like implicit pipes
            // BOX treeIndex, transform -> cadreveal, 


            Console.WriteLine("Hello World!");
        }

        private static CadNode CollectGeometryNodes(RvmNode root, CadNode parent)
        {
            var node = new CadNode
            {
                NodeId = nodeIdGenerator.GetNodeId(null),
                TreeIndex = treeIndexGenerator.GetNextId(),
                Parent = parent,
                Group = root
            };
            node.Children = root.Children.OfType<RvmNode>().Select(n => CollectGeometryNodes(n, node)).ToArray();
            return node;
        }
    }
}