namespace CadRevealComposer.ModelFormatProvider;

using Configuration;
using IdProviders;
using Primitives;
using System.Collections.Generic;
using System.IO;

public interface IModelFormatProvider
{
    IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator);

    public APrimitive[] ProcessGeometries(APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters);
}