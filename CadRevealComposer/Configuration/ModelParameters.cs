namespace CadRevealComposer.Configuration;

/// <summary>
///
/// </summary>
/// <param name="ProjectId"></param>
/// <param name="ModelId"></param>
/// <param name="RevisionId"></param>
/// <param name="InstancingThreshold">The amount of identical Meshes needed to be a candidate for instancing.</param>
public record ModelParameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId, InstancingThreshold InstancingThreshold);

public record ProjectId(long Value);

public record ModelId(long Value);

public record RevisionId(long Value);
public record InstancingThreshold(uint Value);