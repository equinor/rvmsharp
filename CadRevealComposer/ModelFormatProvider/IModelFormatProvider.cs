namespace CadRevealComposer.ModelFormatProvider;

using Configuration;
using IdProviders;
using System.Collections.Generic;
using System.IO;
using Primitives;

public interface IModelFormatProvider
{
    IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator);

    public APrimitive[] ProcessGeometries(APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters);
}