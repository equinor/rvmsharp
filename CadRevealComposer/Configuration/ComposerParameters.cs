namespace CadRevealComposer.Configuration;

public record ComposerParameters(
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones,
    NodeNameExcludeGlobs NodeNameExcludeGlobs
);

public record NodeNameExcludeGlobs(string[] Values);
