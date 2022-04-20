namespace CadRevealComposer.Configuration;

public record ComposerParameters(
    string Mesh2CtmToolPath,
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones);