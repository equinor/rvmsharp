namespace CadRevealComposer.ModelFormatProvider;

using IdProviders;
using System.Collections.Generic;
using System.IO;

public interface IModelFormatProvider
{
    IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator);
}