namespace CadRevealFbxProvider;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Primitives;

public class FbxProvider : IModelFormatProvider
{
    public IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator)
    {
        return null;
    }

    public APrimitive[] ProcessGeometries(APrimitive[] geometries, ComposerParameters composerParameters,
        ModelParameters modelParameters)
    {
        return null;
    }
}
