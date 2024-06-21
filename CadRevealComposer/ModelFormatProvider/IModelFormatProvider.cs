namespace CadRevealComposer.ModelFormatProvider;

using System.Collections.Generic;
using System.IO;
using Configuration;
using IdProviders;
using Operations;
using Primitives;

public interface IModelFormatProvider
{
    (IReadOnlyList<CadRevealNode>, ModelMetadata?) ParseFiles(
        IEnumerable<FileInfo> filesToParse,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering
    );

    public APrimitive[] ProcessGeometries(
        APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters,
        InstanceIdGenerator instanceIdGenerator
    );
}
