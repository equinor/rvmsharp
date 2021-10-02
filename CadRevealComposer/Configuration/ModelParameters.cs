namespace CadRevealComposer.Configuration
{
    public record ModelParameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId);

    public record ProjectId(long Value);

    public record ModelId(long Value);

    public record RevisionId(long Value);
}