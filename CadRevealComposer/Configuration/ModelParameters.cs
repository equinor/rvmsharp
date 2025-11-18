namespace CadRevealComposer.Configuration;

/// <summary>
/// Parameters for the model configuration
/// </summary>
/// <param name="ProjectId"></param>
/// <param name="ModelId"></param>
/// <param name="RevisionId"></param>
/// <param name="InstancingThreshold">The amount of identical Meshes needed to be a candidate for instancing.</param>
/// <param name="TemplateCountLimit">The maximal allowed number of templates exported.</param>
public record ModelParameters(
    ProjectId ProjectId,
    ModelId ModelId,
    RevisionId RevisionId,
    InstancingThreshold InstancingThreshold,
    TemplateCountLimit TemplateCountLimit,
    bool NoPrioritySectors
);

public record ProjectId(long Value);

public record ModelId(long Value);

public record RevisionId(long Value);

public record InstancingThreshold(uint Value);

public record TemplateCountLimit(uint Value);
