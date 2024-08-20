namespace CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;
using CadRevealComposer.Tessellation;

public abstract class ScaffoldPartOptimizer
{
    public abstract string GetName();
    public abstract Mesh[] Optimize(Mesh mesh);
    public abstract string[] GetPartNameTriggerKeywords();
}
