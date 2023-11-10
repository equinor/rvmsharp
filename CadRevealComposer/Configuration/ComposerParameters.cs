namespace CadRevealComposer.Configuration;

using System.IO;

public record ComposerParameters(
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones,
    NodeNameExcludeRegex NodeNameExcludeRegex,
    float SimplificationThreshold,
    DirectoryInfo? DevPrimitiveCacheFolder
);

public record NodeNameExcludeRegex(string? Value);
