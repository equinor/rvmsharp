using System;

namespace CadRevealComposer
{
    using RvmSharp.BatchUtils;
    using RvmSharp.Primitives;
    using System.Linq;

    static class Program
    {
        static readonly TreeIndexGenerator TreeIndexGenerator = new TreeIndexGenerator();
        static readonly NodeIdProvider NodeIdGenerator = new NodeIdProvider();

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            var workload = Workload.CollectWorkload(new[] {@"d:\Models\hda\rvm20210126\"});
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
            {
                Console.WriteLine(x.fileName);
            });
            
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            // Project name og project parameters tull from Cad Control Center
            var rootNode =
                new CadNode
                {
                    NodeId = NodeIdGenerator.GetNodeId(null),
                    TreeIndex = TreeIndexGenerator.GetNextId(),
                    Parent = null,
                    Group = null,
                    Children = null
                };

            rootNode.Children = rvmStore.RvmFiles.SelectMany(f => f.Model.Children)
                .Select(root => CollectGeometryNodesRecursive(root, rootNode)).ToArray();

            // TODO: Nodes must be generated for implicit geometry like implicit pipes
            // BOX treeIndex, transform -> cadreveal, 

            Console.WriteLine("Hello World!");
        }

        private static CadNode CollectGeometryNodesRecursive(RvmNode root, CadNode parent)
        {
            var node = new CadNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Group = root,
                Parent = parent,
                Children = null
            };

            var childrenCadNodes = root.Children.OfType<RvmNode>().Select(n => CollectGeometryNodesRecursive(n, node)).ToArray();
            node.Children = childrenCadNodes;
            return node;
        }
    }
}