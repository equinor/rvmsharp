namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;

using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

public abstract class ScaffoldPartOptimizerTest : IScaffoldPartOptimizer
{
    public abstract string Name { get; }
    public abstract IScaffoldOptimizerResult[] Optimize(
        APrimitive basePrimitive,
        Mesh mesh,
        Func<ulong, int, ulong> requestChildPartInstanceId
    );
    public abstract string[] GetPartNameTriggerKeywords();

    public abstract List<List<Vector3>> GetVerticesTruth();
    public abstract List<List<uint>> GetIndicesTruth();
}
