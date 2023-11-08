namespace CadRevealComposer.Configuration;

public record ComposerParameters(
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones,
    NodeNameExcludeRegex NodeNameExcludeRegex,
    float SimplificationThreshold
);

public record NodeNameExcludeRegex(string? Value);
