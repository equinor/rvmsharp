namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;

using System.Numerics;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

public abstract class ScaffoldPartOptimizerTest : ScaffoldPartOptimizer
{
    public abstract List<Vector3> GetVerticesTruth();
    public abstract List<uint> GetIndicesTruth();
}
