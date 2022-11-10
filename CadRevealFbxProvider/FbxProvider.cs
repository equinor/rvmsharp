namespace CadRevealFbxProvider;

using BatchUtils;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public class FbxProvider : IModelFormatProvider
{
    public IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator)
    {
        using var test = new FbxImporter();

        var RootNode = test.LoadFile(@"D:\Models\FBX\AQ110South-3DView.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, int)>();
        List<APrimitive> geometriesToProcess = new List<APrimitive>();
        var nodesToProcess = FbxWorkload.IterateAndGenerate(RootNode, treeIndexGenerator, test, lookupA, geometriesToProcess).ToList();

        return nodesToProcess;
    }

    public APrimitive[] ProcessGeometries(APrimitive[] geometries, ComposerParameters composerParameters,
        ModelParameters modelParameters)
    {
        return geometries;
    }
}
