namespace CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

using CadRevealComposer.Tessellation;

public interface IScaffoldPartOptimizer
{
    public string Name { get; }
    public Mesh[] Optimize(Mesh mesh);
    public string[] GetPartNameTriggerKeywords();
}
