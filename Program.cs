using Equinor.MeshOptimizationPipeline;
using rvmsharp.Rvm;
using rvmsharp.Tessellator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace rvmsharp
{
    class Program
    {
        


        const string win_path = @"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.RVM";
        static void Main(string[] args)
        {
                      
            var fileStream = File.OpenRead(@"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.RVM");
            var rvm = RvmParser.ReadRvm(fileStream);
            var pdmsData = PdmsTextParser.GetAllPdmsNodesInFile(@"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.txt");

            var store = new RvmStore();
            store.RvmFiles.Add(rvm);
            RvmConnect.Connect(store);
            RvmAlign.Align(store);
            
            AssignRecursive(pdmsData, rvm.Model.children);
            var leafs = rvm.Model.children.SelectMany((c) => CollectGeometryNodes(c)).ToArray();

            var i = 0;
            foreach (var leaf in leafs)
            {
                var found = false;
                using OBJExporter o = new OBJExporter($"E:/testdata{i++}.obj");
                foreach (var p in leaf.Primitives)
                {
                    switch (p) {
                        case RvmBox:
                        case RvmFacetGroup:
                            var mesh = TessellatorBridge.Tessellate(p, 1);
                            mesh.Apply(p.Matrix);
                            found = true;
                            o.WriteMesh(mesh);
                            break;
                    }
                }
                
            }

            Console.WriteLine("Done!");
        }

        private static void AssignRecursive(IList<PdmsTextParser.PdmsNode> attributes, IList<RvmGroup> groups)
        {
            if (attributes.Count != groups.Count)
                Console.Error.WriteLine("Length of attribute nodes does not match group length");
            var copy = new List<RvmGroup>(groups);
            for (var i = 0; i < attributes.Count; i++)
            {
                var pdms = attributes[i];
                for (var k = 0; k < copy.Count; k++)
                {
                    var group = copy[k];
                    if (group.Name == pdms.Name)
                    {
                        // todo attr
                        foreach (var kvp in  pdms.MetadataDict)
                            group.Attributes.Add(kvp.Key, kvp.Value);
                        AssignRecursive(pdms.Children, group.Children);
                        break;
                    }
                }
            }
        }

        private static IEnumerable<RvmGroup> CollectGeometryNodes(RvmGroup root) {
            if (root.Primitives.Count > 0)
                yield return root;
            foreach (var child in root.Children)
                foreach (var p in CollectGeometryNodes(child))
                    yield return p;
        }
    }
}
