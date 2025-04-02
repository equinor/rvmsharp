using BenchmarkDotNet.Attributes;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealFbxProvider;

[MemoryDiagnoser]
public class LoadFbxFileBenchmark
{
    private static readonly string TestFile = new("Data/AA700-MECH-AKO.fbx");

    [Benchmark]
    public void LoadFbxFile()
    {
        using var fbxImporter = new FbxImporter();
        var fbxRootNode = fbxImporter.LoadFile(TestFile);

        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();
        var nodeNameFiltering = new NodeNameFiltering(new NodeNameExcludeRegex(null));

        var rootNode = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            fbxRootNode,
            treeIndexGenerator,
            instanceIdGenerator,
            nodeNameFiltering,
            null
        );
    }
}
