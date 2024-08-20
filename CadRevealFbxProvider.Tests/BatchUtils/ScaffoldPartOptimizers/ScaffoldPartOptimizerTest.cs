namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;
using System.Numerics;

public abstract class ScaffoldPartOptimizerTest : ScaffoldPartOptimizer
{
    public abstract List<Vector3> GetVerticesTruth();
    public abstract List<uint> GetIndicesTruth();
}
