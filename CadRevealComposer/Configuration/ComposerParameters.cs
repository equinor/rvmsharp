namespace CadRevealComposer.Configuration;

public record ComposerParameters(
    bool NoInstancing,
    bool SingleSector,
    bool SplitIntoZones,
    NodeNameExcludeRegex NodeNameExcludeRegex,
    PrioritizedDisciplinesRegex PrioritizedDisciplinesRegex,
    LowPrioritizedDisciplineRegex LowPrioritizedDisciplineRegex,
    PrioritizedNodeNamesRegex PrioritizedNodeNamesRegex
);

public record NodeNameExcludeRegex(string? Value);

public record PrioritizedDisciplinesRegex(string? Value);

public record LowPrioritizedDisciplineRegex(string? Value);

public record PrioritizedNodeNamesRegex(string? Value);
