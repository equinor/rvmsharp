using System.IO;

namespace CadRevealComposer.Configuration;

public enum SplittingStrategy
{
    Octree,
    KdTree,
}

public record ComposerParameters(
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones,
    NodeNameExcludeRegex NodeNameExcludeRegex,
    float SimplificationThreshold,
    DirectoryInfo? DevPrimitiveCacheFolder,
    SplittingStrategy SplittingStrategy = SplittingStrategy.Octree
);

public record NodeNameExcludeRegex(string? Value);
