namespace CadRevealComposer.Configuration
{
    public record ComposerParameters(string Mesh2CtmToolPath,
        bool NoInstancing, bool CreateSingleSector, bool DeterministicOutput);
}