namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;

using System.Numerics;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

public abstract class ScaffoldPartOptimizerTest : IScaffoldPartOptimizer
{
    public abstract string Name { get; }
    public abstract Mesh[] Optimize(Mesh mesh);
    public abstract string[] GetPartNameTriggerKeywords();

    public abstract List<Vector3> GetVerticesTruth();
    public abstract List<uint> GetIndicesTruth();
}
